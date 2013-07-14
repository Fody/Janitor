using Mono.Cecil;
using Mono.Cecil.Cil;

public class OnlyUnmangedProcessor
{
    public TypeProcessor TypeProcessor;
    public MethodDefinition DisposeUnmanagedMethod;
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
            Instruction.Create(OpCodes.Call, disposeBoolMethod),
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
            Instruction.Create(OpCodes.Call, DisposeUnmanagedMethod),
            Instruction.Create(OpCodes.Ret));
        TypeProcessor.TargetType.Methods.Add(disposeBoolMethod);
        TypeProcessor.AddFinalizer(disposeBoolMethod);
    }




}