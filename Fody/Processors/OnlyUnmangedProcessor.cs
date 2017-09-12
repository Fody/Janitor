using Mono.Cecil;
using Mono.Cecil.Cil;

public class OnlyUnmanagedProcessor
{
    public TypeProcessor TypeProcessor;
    public MethodReference DisposeUnmanagedMethod;
    MethodDefinition disposeBoolMethod;

    public void Process()
    {
        CreateDisposeBoolMethod();
        InjectIntoDispose();
    }

    void InjectIntoDispose()
    {
        var instructions = TypeProcessor.DisposeMethod.Body.Instructions;
        instructions.Clear();
        instructions.Add(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Ldc_I4_1),
            Instruction.Create(OpCodes.Call, disposeBoolMethod.GetGeneric()),
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, TypeProcessor.ModuleWeaver.SuppressFinalizeMethodReference),
            Instruction.Create(OpCodes.Ret));
    }

    void CreateDisposeBoolMethod()
    {
        var typeSystem = TypeProcessor.ModuleWeaver.ModuleDefinition.TypeSystem;
        disposeBoolMethod = new MethodDefinition("Dispose", MethodAttributes.HideBySig | MethodAttributes.Private, typeSystem.Void);
        var disposingParameter = new ParameterDefinition("disposing", ParameterAttributes.None, typeSystem.Boolean);
        disposeBoolMethod.Parameters.Add(disposingParameter);

        var instructions = disposeBoolMethod.Body.Instructions;
        instructions.Add(TypeProcessor.GetDisposeEscapeInstructions());

        instructions.Add(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(DisposeUnmanagedMethod.GetCallingConvention(), DisposeUnmanagedMethod));
        instructions.Add(TypeProcessor.GetDisposedInstructions());
        instructions.Add(Instruction.Create(OpCodes.Ret));

        TypeProcessor.TargetType.Methods.Add(disposeBoolMethod);
        TypeProcessor.AddFinalizer(disposeBoolMethod);
    }




}