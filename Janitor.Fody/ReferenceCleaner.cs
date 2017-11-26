using System.Linq;

public partial class ModuleWeaver
{
    public void CleanReferences()
    {
        ModuleDefinition.Assembly.CustomAttributes.RemoveSkipWeavingNamespace();
        foreach (var typeDefinition in ModuleDefinition.GetTypes())
        {
            typeDefinition.CustomAttributes.RemoveSkipWeaving();
            foreach (var field in typeDefinition.Fields)
            {
                field.CustomAttributes.RemoveSkipWeaving();
            }
        }

        var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "Janitor");
        if (referenceToRemove == null)
        {
            LogInfo("\tNo reference to 'Janitor' found. References not modified.");
            return;
        }

        ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
        LogInfo("\tRemoving reference to 'Janitor'.");
    }
}