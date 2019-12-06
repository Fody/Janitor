using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using TypeSystem = Fody.TypeSystem;

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
        typeSystem = ModuleWeaver.TypeSystem;
        var disposeManagedMethod = TargetType.Methods
            .FirstOrDefault(x => !x.IsStatic && x.IsMatch("DisposeManaged"));
        var disposeUnmanagedMethod = TargetType.Methods
            .FirstOrDefault(x => !x.IsStatic && x.IsMatch("DisposeUnmanaged"));

        if (disposeUnmanagedMethod != null && disposeManagedMethod == null)
        {
            disposeManagedMethod = CreateDisposeManagedIfNecessary();
        }

        if (TargetType.FieldExists("disposeSignaled"))
        {
            ModuleWeaver.LogError($"Type `{TargetType.FullName}` contains a `disposeSignaled` field. Either remove this field or add a `[Janitor.SkipWeaving]` attribute to the type.");
            return;
        }

        if (TargetType.FieldExists("disposed"))
        {
            ModuleWeaver.LogError($"Type `{TargetType.FullName}` contains a `disposed` field. Either remove this field or add a `[Janitor.SkipWeaving]` attribute to the type.");
            return;
        }

        if (TargetType.MethodExists("ThrowIfDisposed"))
        {
            ModuleWeaver.LogError($"Type `{TargetType.FullName}` contains a `ThrowIfDisposed` method. Either remove this method or add a `[Janitor.SkipWeaving]` attribute to the type.");
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
        var finalizeMethod = new MethodDefinition("Finalize", MethodAttributes.HideBySig | MethodAttributes.Family | MethodAttributes.Virtual, typeSystem.VoidReference);
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
        if (instructions.Count == 0)
        {
            return null;
        }
        var disposeManagedMethod = new MethodDefinition("DisposeManaged", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.VoidReference);
        TargetType.Methods.Add(disposeManagedMethod);
        var collection = disposeManagedMethod.Body.Instructions;
        collection.Add(instructions);
        collection.Add(Instruction.Create(OpCodes.Ret));
        return disposeManagedMethod;
    }

    public IEnumerable<Instruction> GetDisposeEscapeInstructions()
    {
        var skipReturnInstruction = Instruction.Create(OpCodes.Nop);
        yield return Instruction.Create(OpCodes.Ldarg_0);
        yield return Instruction.Create(OpCodes.Ldflda, signaledField);
        yield return Instruction.Create(OpCodes.Ldc_I4_1);
        yield return Instruction.Create(OpCodes.Call, ModuleWeaver.ExchangeIntMethodReference);
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
            if (field == disposedField)
            {
                continue;
            }
            if (field == signaledField)
            {
                continue;
            }
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
                ModuleWeaver.LogError($"Could not add dispose for field '{field.GetName()}' since it is marked as readonly. Change this field to not be readonly.");
                continue;
            }
            if (field.FieldType.IsValueType)
            {
                ModuleWeaver.LogError($"Could not add dispose for field '{field.GetName()}' since it is a value type.");
                continue;
            }

            if (field.FieldType.HasGenericParameters || TargetType.HasGenericParameters)
            {
                ModuleWeaver.LogError($"Could not add dispose for field '{field.GetName()}' since it has generic parameters.");
                continue;
            }
            if (field.FieldType.FullName.StartsWith("System.Threading.Tasks.Task"))
            {
                // do not dispose tasks, see https://blogs.msdn.microsoft.com/pfxteam/2012/03/25/do-i-need-to-dispose-of-tasks/
                continue;
            }

            var exchangedMethodReference = ModuleWeaver.ExchangeTMethodReference
                .MakeGeneric(field.FieldType.GetDisposable());

            var br1 = Instruction.Create(OpCodes.Callvirt, ModuleWeaver.DisposeMethodReference);
            var br2 = Instruction.Create(OpCodes.Nop);
            yield return Instruction.Create(OpCodes.Ldarg_0);
            yield return Instruction.Create(OpCodes.Ldflda, field.GetGeneric());
            yield return Instruction.Create(OpCodes.Ldnull);
            yield return Instruction.Create(OpCodes.Call, exchangedMethodReference);
            yield return Instruction.Create(OpCodes.Dup);
            yield return Instruction.Create(OpCodes.Brtrue, br1);
            yield return Instruction.Create(OpCodes.Pop);
            yield return Instruction.Create(OpCodes.Br, br2);
            yield return br1;
            yield return br2;
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

            var validSequencePoint = method.DebugInformation.SequencePoints.FirstOrDefault();
            method.Body.SimplifyMacros();
            var instructions = method.Body.Instructions;
            instructions.InsertAtStart(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, throwIfDisposed));
            if (validSequencePoint != null)
            {
                CecilExtensions.HideLineFromDebugger(validSequencePoint);
            }
            method.Body.OptimizeMacros();
        }
    }

    void CreateSignaledField()
    {
        var field = new FieldDefinition("disposeSignaled", FieldAttributes.Private, typeSystem.Int32Reference);
        TargetType.Fields.Add(field);
        signaledField = field.GetGeneric();
    }

    void CreateDisposedField()
    {
        var field = new FieldDefinition("disposed", FieldAttributes.Private, typeSystem.BooleanReference);
        TargetType.Fields.Add(field);
        disposedField = field.GetGeneric();
    }

    public void CreateThrowIfDisposed()
    {
        var method = new MethodDefinition("ThrowIfDisposed", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.VoidReference);
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
