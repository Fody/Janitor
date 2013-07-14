using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class TypeProcessor
{
    public MethodDefinition DisposeMethod;
    public Action<string> LogInfo;
    public ModuleWeaver ModuleWeaver;
    public TypeDefinition TargetType;
    FieldDefinition isDisposedField;
    TypeSystem typeSystem;

    public void Process()
    {
        typeSystem = ModuleWeaver.ModuleDefinition.TypeSystem;
        var disposeManagedMethod = TargetType.Methods
                                             .FirstOrDefault(x => !x.IsStatic && x.IsMatch("DisposeManaged"));
        var disposeUnmanagedMethod = TargetType.Methods
                                               .FirstOrDefault(x => !x.IsStatic && x.IsMatch("DisposeUnmanaged"));

        if (disposeUnmanagedMethod != null && disposeManagedMethod == null)
        {
            disposeManagedMethod = CreateDisposeManagedIfNecessary();
        }

        CreateIsDisposedField();

        if (disposeUnmanagedMethod == null && disposeManagedMethod == null)
        {
            var methodProcessor = new SimpleDisposeProcessor
                                  {
                                      TypeProcessor = this
                                  };
            methodProcessor.Process();
        }
        else if (disposeUnmanagedMethod == null)
        {
            var methodProcessor = new OnlyManagedProcessor
                                  {
                                      TypeProcessor = this,
                                      DisposeManagedMethod = disposeManagedMethod
                                  };
            methodProcessor.Process();

        }
        else if (disposeManagedMethod == null)
        {
            var methodProcessor = new OnlyUnmangedProcessor
                                  {
                                      TypeProcessor = this,
                                      DisposeUnmanagedMethod = disposeUnmanagedMethod
                                  };
            methodProcessor.Process();
        }
        else
        {
            var methodProcessor = new ManagedAndUnmanagedProcessor
                                  {
                                      TypeProcessor = this,
                                      DisposeUnmanagedMethod = disposeUnmanagedMethod,
                                      DisposeManagedMethod = disposeManagedMethod
                                  };
            methodProcessor.Process();
        }
        AddGuards();
    }

    public void AddFinalizer(MethodDefinition disposeBoolMethod)
    {
        if (TargetType.Methods.Any(x => !x.IsStatic && x.IsMatch("Finalize")))
        {
            //TODO: should support injecting into existing finalizer
            return;
        }
        var finalizeMethod = new MethodDefinition("Finalize", MethodAttributes.HideBySig | MethodAttributes.Family | MethodAttributes.Virtual, typeSystem.Void);
        var instructions = finalizeMethod.Body.Instructions;

        var ret = Instruction.Create(OpCodes.Ret);

        var tryStart = Instruction.Create(OpCodes.Ldarg_0);
        instructions.Add(tryStart);
        if (disposeBoolMethod.Parameters.Count == 1)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        }
        instructions.Add(Instruction.Create(OpCodes.Call, disposeBoolMethod));
        instructions.Add(Instruction.Create(OpCodes.Leave, ret));
        var tryEnd = Instruction.Create(OpCodes.Ldarg_0);
        instructions.Add(tryEnd);
        instructions.Add(Instruction.Create(OpCodes.Call, ModuleWeaver.ObjectFinalizeReference));
        instructions.Add(Instruction.Create(OpCodes.Endfinally));
        instructions.Add(ret);

        var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                             {
                                 TryStart = tryStart,
                                 TryEnd = tryEnd,
                                 HandlerStart = tryEnd, 
                                 HandlerEnd = ret
                             };

        finalizeMethod.Body.ExceptionHandlers.Add(finallyHandler);
        TargetType.Methods.Add(finalizeMethod);
    }

    public MethodDefinition CreateDisposeManagedIfNecessary()
    {
        var instructions = GetDisposeOfFieldInstructions().ToList();
        if (instructions.Count > 0)
        {
            var disposeManagedMethod = new MethodDefinition("DisposeManaged", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.Void);
            TargetType.Methods.Add(disposeManagedMethod);
            var collection = disposeManagedMethod.Body.Instructions;
            collection.Add(instructions);
            collection.Add(Instruction.Create(OpCodes.Ret));
            return disposeManagedMethod;
        }
        return null;
    }


  public  IEnumerable<Instruction> GetDisposeEscapeInstructions()
    {
        var skipReturnInstruction = Instruction.Create(OpCodes.Ldarg_0);
        yield return Instruction.Create(OpCodes.Ldarg_0);
        yield return Instruction.Create(OpCodes.Ldfld, isDisposedField);
        yield return Instruction.Create(OpCodes.Brfalse, skipReturnInstruction);
        yield return Instruction.Create(OpCodes.Ret);
        yield return skipReturnInstruction;
        yield return Instruction.Create(OpCodes.Ldc_I4_1);
        yield return Instruction.Create(OpCodes.Stfld, isDisposedField);
    }


   public IEnumerable<Instruction> GetDisposeOfFieldInstructions()
    {
        foreach (var field in TargetType.Fields.Reverse())
        {
            if (field.CustomAttributes.ContainsSkipWeaving())
            {
                continue;
            }
            if (field.IsStatic)
            {
                continue;
            }
            if (!field.FieldType.IsIDisposable())
            {
                continue;
            }

            var skip = Instruction.Create(OpCodes.Nop);
            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldfld, field);
            yield return Instruction.Create(OpCodes.Brfalse, skip);
            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldfld, field);
            yield return Instruction.Create(OpCodes.Callvirt, ModuleWeaver.DisposeMethodReference);
            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldnull);
            yield return Instruction.Create(OpCodes.Stfld, field);
            yield return skip;
        }
    }

    void AddGuards()
    {
        foreach (var method in TargetType.Methods)
        {
            if (method.Name ==".ctor")
            {
                continue;
            }
            if (method.IsMatch("Finalize"))
            {
                continue;
            }
            if (method.IsStatic)
            {
                continue;
            }
            if (method.Name.StartsWith("Dispose"))
            {
                continue;
            }
            if (!method.HasBody)
            {
                continue;
            }

            method.Body.SimplifyMacros();
            var instructions = method.Body.Instructions;
            var first = instructions.First();
            instructions.InsertAtStart(new []
                                       {
                                           Instruction.Create(OpCodes.Ldarg_0), 
                                           Instruction.Create(OpCodes.Ldfld, isDisposedField), 
                                           Instruction.Create(OpCodes.Brfalse, first), 
                                           Instruction.Create(OpCodes.Ldstr, TargetType.FullName), 
                                           Instruction.Create(OpCodes.Newobj, ModuleWeaver.ExceptionConstructorReference), 
                                           Instruction.Create(OpCodes.Throw), 
                                       });
            method.Body.OptimizeMacros();
        }
    }

    void CreateIsDisposedField()
    {
        if (TargetType.Fields.FirstOrDefault(x => x.Name == "isDisposed") != null)
        {
            var message = string.Format("Type `{0}` contains a `isDisposed` field. Either remove this field or add a `[Janitor.SkipWeaving]` attribute to the type.", TargetType.FullName);
            throw new WeavingException(message);
        }
        isDisposedField = new FieldDefinition("isDisposed", FieldAttributes.Private, typeSystem.Boolean);
        TargetType.Fields.Add(isDisposedField);
    }

}