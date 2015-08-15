using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{

    public void FindCoreReferences()
    {
        var assemblyResolver = ModuleDefinition.AssemblyResolver;
        var msCoreLibDefinition = assemblyResolver.Resolve("mscorlib");
        var msCoreTypes = msCoreLibDefinition.MainModule.Types;

        ObjectFinalizeReference = ModuleDefinition.ImportReference(ModuleDefinition.TypeSystem.Object.Resolve().Find("Finalize"));

        var gcTypeDefinition = msCoreTypes.First(x => x.Name == "GC");
        SuppressFinalizeMethodReference = ModuleDefinition.ImportReference(gcTypeDefinition.Find("SuppressFinalize", "Object"));
        
        var interlockedTypeDefinition = msCoreTypes.First(x => x.Name == "Interlocked");
        ExchangeMethodReference = ModuleDefinition.ImportReference(interlockedTypeDefinition.Find("Exchange", "Int32&", "Int32"));

        var exceptionTypeDefinition = msCoreTypes.First(x => x.Name == "ObjectDisposedException");
        ExceptionConstructorReference = ModuleDefinition.ImportReference(exceptionTypeDefinition.Find(".ctor", "String"));

        var iDisposableTypeDefinition = msCoreTypes.First(x => x.Name == "IDisposable");
        DisposeMethodReference = ModuleDefinition.ImportReference(iDisposableTypeDefinition.Find("Dispose"));
    }

    public MethodReference ExchangeMethodReference;
    public MethodReference SuppressFinalizeMethodReference;
    public MethodReference ObjectFinalizeReference;
    public MethodReference DisposeMethodReference;
    public MethodReference ExceptionConstructorReference;
}