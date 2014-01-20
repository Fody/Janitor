using System;
using System.Linq;
using Mono.Cecil;

public partial class ModuleWeaver
{
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarning { get; set; }
    public Action<string> LogError { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogWarning = s => { };
        LogError = s => { };
    }

    public void Execute()
    {
        LoggerFactory.LogInfo = LogInfo;
        LoggerFactory.LogWarn = LogWarning;
        LoggerFactory.LogError = LogError;

        FindCoreReferences();

        foreach (var type in ModuleDefinition
            .GetTypes()
            .Where(x =>
                x.IsClass() &&
                !x.IsAbstract &&
                !x.IsGeneratedCode() &&
                !x.CustomAttributes.ContainsSkipWeaving()))
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
                var message = string.Format("Type `{0}` contains more than one `Dispose` method. Either remove one or add a `[Janitor.SkipWeaving]` attribute to the type.", type.FullName);
                LogError(message);
            }
            var disposeMethod = disposeMethods.First();

            if (!disposeMethod.IsEmptyOrNotImplemented())
            {
                var message = string.Format("Type `{0}` contains a `Dispose` method with code. Either remove the code or add a `[Janitor.SkipWeaving]` attribute to the type.", type.FullName);
                LogError(message);
            }
            if (type.BaseType.Name != "Object")
            {
                var message = string.Format("Type `{0}` has a base class which is not currently supported. Either remove the base class or add a `[Janitor.SkipWeaving]` attribute to the type.", type.FullName);
                LogError(message);
            }

            var methodProcessor = new TypeProcessor
                                  {
                                      DisposeMethod = disposeMethod,
                                      ModuleWeaver = this,
                                      TargetType = type,
                                      LogInfo = LogInfo
                                  };
            methodProcessor.Process();
        }
        CleanReferences();
    }


}