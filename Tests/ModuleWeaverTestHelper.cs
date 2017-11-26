using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using System.Collections.Generic;

public class ModuleWeaverTestHelper
{
    public string BeforeAssemblyPath;
    public string AfterAssemblyPath;
    public Assembly Assembly;
    public List<string> Errors;

    public ModuleWeaverTestHelper(string inputAssembly)
    {
        BeforeAssemblyPath = Path.GetFullPath(inputAssembly);
        AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
        var oldPdb = BeforeAssemblyPath.Replace(".dll", ".pdb");
        var newPdb = BeforeAssemblyPath.Replace(".dll", "2.pdb");
        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
        File.Copy(oldPdb, newPdb, true);

        Errors = new List<string>();

        using (var assemblyResolver = new DefaultAssemblyResolver())
        {
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(BeforeAssemblyPath));

            using (var symbolStream = File.OpenRead(newPdb))
            {
                var readerParameters = new ReaderParameters
                {
                    ReadSymbols = true,
                    SymbolStream = symbolStream,
                    SymbolReaderProvider = new PdbReaderProvider()
                };
                using (var moduleDefinition = ModuleDefinition.ReadModule(BeforeAssemblyPath, readerParameters))
                {
                    var weavingTask = new ModuleWeaver
                    {
                        ModuleDefinition = moduleDefinition,
                        AssemblyResolver = assemblyResolver,
                        LogError = s => Errors.Add(s),
                    };

                    weavingTask.Execute();
                    moduleDefinition.Write(AfterAssemblyPath);
                }
            }
        }
        Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }
}