using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    public void FindCoreReferences()
    {
        List<TypeDefinition> types = new List<TypeDefinition>();
        AddAssemblyIfExists("mscorlib", types);
        AddAssemblyIfExists("System.Runtime", types);
        AddAssemblyIfExists("System.Threading", types);

        ObjectFinalizeReference = ModuleDefinition.ImportReference(ModuleDefinition.TypeSystem.Object.Resolve().Find("Finalize"));

        var gcTypeDefinition = types.First(x => x.Name == "GC");
        SuppressFinalizeMethodReference = ModuleDefinition.ImportReference(gcTypeDefinition.Find("SuppressFinalize", "Object"));

        var iDisposableTypeDefinition = types.First(x => x.Name == "IDisposable");
        DisposeMethodReference = ModuleDefinition.ImportReference(iDisposableTypeDefinition.Find("Dispose"));

        var interlockedTypeDefinition = types.First(x => x.Name == "Interlocked");
        ExchangeIntMethodReference = ModuleDefinition.ImportReference(interlockedTypeDefinition.Find("Exchange", "Int32&", "Int32"));
        ExchangeTMethodReference = ModuleDefinition.ImportReference(interlockedTypeDefinition.Find("Exchange", "T&", "T"));

        var exceptionTypeDefinition = types.First(x => x.Name == "ObjectDisposedException");
        ExceptionConstructorReference = ModuleDefinition.ImportReference(exceptionTypeDefinition.Find(".ctor", "String"));
    }

    void AddAssemblyIfExists(string name, List<TypeDefinition> types)
    {
        var msCoreLibDefinition = ModuleDefinition.AssemblyResolver.Resolve(new AssemblyNameReference(name, null));
        if (msCoreLibDefinition != null)
        {
            types.AddRange(msCoreLibDefinition.MainModule.Types);
        }
    }

    public MethodReference ExchangeIntMethodReference;
    public MethodReference ExchangeTMethodReference;
    public MethodReference SuppressFinalizeMethodReference;
    public MethodReference ObjectFinalizeReference;
    public MethodReference DisposeMethodReference;
    public MethodReference ExceptionConstructorReference;
}