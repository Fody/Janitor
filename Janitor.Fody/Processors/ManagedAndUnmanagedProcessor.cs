using Mono.Cecil;
using Mono.Cecil.Cil;

public class ManagedAndUnmanagedProcessor
{
    MethodDefinition DisposeBoolMethod;
    TypeSystem typeSystem;
    public TypeProcessor TypeProcessor;
    public MethodReference DisposeManagedMethod;
    public MethodReference DisposeUnmanagedMethod;

    public void Process()
    {
        typeSystem = TypeProcessor.ModuleWeaver.ModuleDefinition.TypeSystem;
        CreateDisposeBoolMethod();
        InjectIntoDispose();
        TypeProcessor.AddFinalizer(DisposeBoolMethod);
    }

    void InjectIntoDispose()
    {
        var instructions = TypeProcessor.DisposeMethod.Body.Instructions;
        instructions.Clear();
        instructions.Add(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Call, DisposeBoolMethod.GetGeneric()),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, TypeProcessor.ModuleWeaver.SuppressFinalizeMethodReference),
            Instruction.Create(OpCodes.Ret)
            );
    }

    void CreateDisposeBoolMethod()
    {
        DisposeBoolMethod = new MethodDefinition("Dispose", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.Void);
        var disposingParameter = new ParameterDefinition("disposing", ParameterAttributes.None, typeSystem.Boolean);
        DisposeBoolMethod.Parameters.Add(disposingParameter);

        var instructions = DisposeBoolMethod.Body.Instructions;
     //   instructions.Add(TypeProcessor.GetDisposeEscapeInstructions());

        var skipDisposeManaged = Instruction.Create(OpCodes.Nop);
        instructions.Add(
            Instruction.Create(OpCodes.Ldarg, disposingParameter),
            Instruction.Create(OpCodes.Brfalse, skipDisposeManaged),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(DisposeManagedMethod.GetCallingConvention(), DisposeManagedMethod),
            skipDisposeManaged,
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(DisposeUnmanagedMethod.GetCallingConvention(), DisposeUnmanagedMethod)
            );

        instructions.Add(TypeProcessor.GetDisposedInstructions());
        instructions.Add(Instruction.Create(OpCodes.Ret));

        TypeProcessor.TargetType.Methods.Add(DisposeBoolMethod);
    }
}