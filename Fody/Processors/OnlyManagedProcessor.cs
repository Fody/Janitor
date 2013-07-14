using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class OnlyManagedProcessor
{
    public Action<string> LogInfo;
    public TypeProcessor TypeProcessor;
    public MethodDefinition DisposeManagedMethod;

    public void Process()
    {
        var instructions =TypeProcessor.DisposeMethod.Body.Instructions;
        instructions.Clear();
        instructions.Add(TypeProcessor.GetDisposeEscapeInstructions());
        instructions.Add(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(OpCodes.Call, DisposeManagedMethod),
            Instruction.Create(OpCodes.Ret));
    }
}