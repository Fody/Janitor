using Mono.Cecil;

public partial class ModuleWeaver
{
    public void FindCoreReferences()
    {
        var objectTypeDefinition = FindType("System.Object");
        ObjectFinalizeReference = ModuleDefinition.ImportReference(objectTypeDefinition.Find("Finalize"));

        var gcTypeDefinition = FindType("System.GC");
        SuppressFinalizeMethodReference = ModuleDefinition.ImportReference(gcTypeDefinition.Find("SuppressFinalize", "Object"));

        var iDisposableTypeDefinition = FindType("System.IDisposable");
        DisposeMethodReference = ModuleDefinition.ImportReference(iDisposableTypeDefinition.Find("Dispose"));

        var interlockedTypeDefinition = FindType("System.Threading.Interlocked");
        ExchangeIntMethodReference = ModuleDefinition.ImportReference(interlockedTypeDefinition.Find("Exchange", "Int32&", "Int32"));
        ExchangeTMethodReference = ModuleDefinition.ImportReference(interlockedTypeDefinition.Find("Exchange", "T&", "T"));

        var exceptionTypeDefinition = FindType("System.ObjectDisposedException");
        ExceptionConstructorReference = ModuleDefinition.ImportReference(exceptionTypeDefinition.Find(".ctor", "String"));
    }

    public MethodReference ExchangeIntMethodReference;
    public MethodReference ExchangeTMethodReference;
    public MethodReference SuppressFinalizeMethodReference;
    public MethodReference ObjectFinalizeReference;
    public MethodReference DisposeMethodReference;
    public MethodReference ExceptionConstructorReference;
}