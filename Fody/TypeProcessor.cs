using System.Collections.Generic;
using System.Linq;
using Anotar.Custom;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class TypeProcessor
{
    public MethodDefinition DisposeMethod;
    public ModuleWeaver ModuleWeaver;
    public TypeDefinition TargetType;
    TypeSystem typeSystem;
    FieldReference signaledField;
    FieldReference disposedField;
    MethodReference throwIfDisposed;

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

        var error = TargetType.FieldExists("disposeSignaled") |
                    TargetType.FieldExists("disposed") |
                    TargetType.MethodExists("ThrowIfDisposed");
        if (error)
        {
            return;
        }

        CreateSignaledField();
        CreateDisposedField();
        CreateThrowIfDisposed();

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
                                      DisposeManagedMethod = disposeManagedMethod.GetGeneric()
                                  };
            methodProcessor.Process();

        }
        else if (disposeManagedMethod == null)
        {
            var methodProcessor = new OnlyUnmanagedProcessor
                                  {
                                      TypeProcessor = this,
                                      DisposeUnmanagedMethod = disposeUnmanagedMethod.GetGeneric()
                                  };
            methodProcessor.Process();
        }
        else
        {
            var methodProcessor = new ManagedAndUnmanagedProcessor
                                  {
                                      TypeProcessor = this,
                                      DisposeUnmanagedMethod = disposeUnmanagedMethod.GetGeneric(),
                                      DisposeManagedMethod = disposeManagedMethod.GetGeneric()
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


    public IEnumerable<Instruction> GetDisposeEscapeInstructions()
    {
        var skipReturnInstruction = Instruction.Create(OpCodes.Nop);
        yield return Instruction.Create(OpCodes.Ldarg_0);
        yield return Instruction.Create(OpCodes.Ldflda, signaledField);
        yield return Instruction.Create(OpCodes.Ldc_I4_1);
        yield return Instruction.Create(OpCodes.Call, ModuleWeaver.ExchangeMethodReference);
        yield return Instruction.Create(OpCodes.Brfalse_S, skipReturnInstruction);
        yield return Instruction.Create(OpCodes.Ret);
        yield return skipReturnInstruction;
    }

    public IEnumerable<Instruction> GetDisposedInstructions()
    {
        yield return Instruction.Create(OpCodes.Ldarg_0);
        yield return Instruction.Create(OpCodes.Ldc_I4_1);
        yield return Instruction.Create(OpCodes.Stfld, disposedField);
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
            if (field.IsInitOnly)
            {
                LogTo.Error("Could not add dispose for field '{0}' since it is marked as readonly. Please change this field to not be readonly.", field.GetName());
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
            if (method.Name == ".ctor")
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
            if (method.Name == "ThrowIfDisposed")
            {
                continue;
            }
            if (method.Name == "IsDisposed")
            {
                continue;
            }
            if (!method.HasBody)
            {
                continue;
            }
            if (method.IsPrivate)
            {
                continue;
            }

            var validSequencePoint = method.Body.Instructions.Select(i => i.SequencePoint).FirstOrDefault(sp => sp != null);

            method.Body.SimplifyMacros();
            var instructions = method.Body.Instructions;
            instructions.InsertAtStart(new[]
                                       {
                                           Instruction.Create(OpCodes.Ldarg_0),
                                           Instruction.Create(OpCodes.Call, throwIfDisposed),
                                       });
            if (validSequencePoint != null)
                instructions[0].HideLineFromDebugger(validSequencePoint);
            method.Body.OptimizeMacros();
        }
    }


    void CreateSignaledField()
    {
        var field = new FieldDefinition("disposeSignaled", FieldAttributes.Private, typeSystem.Int32);
        TargetType.Fields.Add(field);
        signaledField = field.GetGeneric();
    }

    void CreateDisposedField()
    {
        var field = new FieldDefinition("disposed", FieldAttributes.Private, typeSystem.Boolean);
        TargetType.Fields.Add(field);
        disposedField = field.GetGeneric();
    }

    public void CreateThrowIfDisposed()
    {
        var method = new MethodDefinition("ThrowIfDisposed", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.Void);
        TargetType.Methods.Add(method);
        var collection = method.Body.Instructions;
        var returnInstruction = Instruction.Create(OpCodes.Ret);
        collection.Add(Instruction.Create(OpCodes.Ldarg_0));
        collection.Add(Instruction.Create(OpCodes.Ldfld, disposedField));
        collection.Add(Instruction.Create(OpCodes.Brfalse_S, returnInstruction));
        collection.Add(Instruction.Create(OpCodes.Ldstr, TargetType.Name));
        collection.Add(Instruction.Create(OpCodes.Newobj, ModuleWeaver.ExceptionConstructorReference));
        collection.Add(Instruction.Create(OpCodes.Throw));
        collection.Add(returnInstruction);

        throwIfDisposed = method.GetGeneric();
    }
}