using Mono.Cecil.Cil;

public class SimpleDisposeProcessor
{
    public TypeProcessor TypeProcessor;

    public void Process()
    {
        var disposeMethod = TypeProcessor.DisposeMethod;
        var instructions = disposeMethod.Body.Instructions;
        instructions.Clear();
        instructions.Add(TypeProcessor.GetDisposeEscapeInstructions());
        instructions.Add(TypeProcessor.GetDisposeOfFieldInstructions());
        instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}