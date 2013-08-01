using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{

    public void FindCoreReferences()
    {
        var assemblyResolver = ModuleDefinition.AssemblyResolver;
        var msCoreLibDefinition = assemblyResolver.Resolve("mscorlib");
        var msCoreTypes = msCoreLibDefinition.MainModule.Types;

        ObjectFinalizeReference = ModuleDefinition.Import(ModuleDefinition.TypeSystem.Object.Resolve().Find("Finalize"));

        var isVolatileTypeDefinition = msCoreTypes.First(x => x.Name == "IsVolatile");
        IsVolatileReference = ModuleDefinition.Import(isVolatileTypeDefinition);

        var gcTypeDefinition = msCoreTypes.First(x => x.Name == "GC");
        SuppressFinalizeMethodReference = ModuleDefinition.Import(gcTypeDefinition.Find("SuppressFinalize", "Object"));
        var interlockedTypeDefinition = msCoreTypes.First(x => x.Name == "Interlocked");
        ExchangeMethodReference = ModuleDefinition.Import(interlockedTypeDefinition.Find("Exchange", "Int32&", "Int32"));

        
        var exceptionTypeDefinition = msCoreTypes.First(x => x.Name == "ObjectDisposedException");
		ExceptionConstructorReference = ModuleDefinition.Import(exceptionTypeDefinition.Find(".ctor", "String"));
        var iDisposableTypeDefinition = msCoreTypes.First(x => x.Name == "IDisposable");
        DisposeMethodReference = ModuleDefinition.Import(iDisposableTypeDefinition.Find("Dispose"));
    }

    public MethodReference ExchangeMethodReference;

    public TypeReference IsVolatileReference;

    public MethodReference SuppressFinalizeMethodReference;

    public MethodReference ObjectFinalizeReference;

    public MethodReference DisposeMethodReference;
    public MethodReference ExceptionConstructorReference;

}