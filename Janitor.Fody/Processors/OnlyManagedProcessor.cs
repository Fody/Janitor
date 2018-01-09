using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

public class OnlyManagedProcessor
{
    public Action<string> LogInfo;
    public TypeProcessor TypeProcessor;
    public MethodReference DisposeManagedMethod;

    public void Process()
    {
        var instructions =TypeProcessor.DisposeMethod.Body.Instructions;
        instructions.Clear();
        instructions.Add(TypeProcessor.GetDisposeEscapeInstructions());
        instructions.Add(
            Instruction.Create(OpCodes.Ldarg_0),
            Instruction.Create(DisposeManagedMethod.GetCallingConvention(), DisposeManagedMethod));
        instructions.Add(TypeProcessor.GetDisposedInstructions());
        instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}