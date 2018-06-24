using System.Collections.Generic;
using System.Linq;
using Fody;

public partial class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        FindCoreReferences();

        var namespacesToSkip = GetNamespacesToSkip();

        foreach (var type in ModuleDefinition
            .GetTypes()
            .Where(x =>
                x.IsClass() &&
                !x.IsGeneratedCode() &&
                !x.CustomAttributes.ContainsSkipWeaving() &&
                !namespacesToSkip.Contains(x.Namespace)))
        {
            var disposeMethods = type.Methods
                .Where(x => !x.IsStatic && (x.Name == "Dispose" || x.Name == "System.IDisposable.Dispose"))
                .ToList();
            if (disposeMethods.Count == 0)
            {
                continue;
            }

            if (disposeMethods.Count > 1)
            {
                var message = $"Type `{type.FullName}` contains more than one `Dispose` method. Either remove one or add a `[Janitor.SkipWeaving]` attribute to the type.";
                LogError(message);
            }

            var disposeMethod = disposeMethods.First();

            if (!disposeMethod.IsEmptyOrNotImplemented())
            {
                var message = $"Type `{type.FullName}` contains a `Dispose` method with code. Either remove the code or add a `[Janitor.SkipWeaving]` attribute to the type.";
                LogError(message);
            }

            if (type.BaseType.Name != "Object")
            {
                var message = $"Type `{type.FullName}` has a base class which is not currently supported. Either remove the base class or add a `[Janitor.SkipWeaving]` attribute to the type.";
                LogError(message);
            }

            var methodProcessor = new TypeProcessor
            {
                DisposeMethod = disposeMethod,
                ModuleWeaver = this,
                TargetType = type,
            };
            methodProcessor.Process();
        }

        ModuleDefinition.Assembly.CustomAttributes.RemoveSkipWeavingNamespace();
        foreach (var typeDefinition in ModuleDefinition.GetTypes())
        {
            typeDefinition.CustomAttributes.RemoveSkipWeaving();
            foreach (var field in typeDefinition.Fields)
            {
                field.CustomAttributes.RemoveSkipWeaving();
            }
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "System.Threading";
    }

    public override bool ShouldCleanReference => true;

    HashSet<string> GetNamespacesToSkip()
    {
        var collection = ModuleDefinition.Assembly.CustomAttributes
            .Where(a => a.AttributeType.FullName == "Janitor.SkipWeavingNamespace")
            .Select(a => (string)a.ConstructorArguments[0].Value);
        return new HashSet<string>(collection);
    }
}