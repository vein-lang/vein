namespace vein.pipes;

using fs;
using MoreLinq;

[ExcludeFromCodeCoverage]
public class WriteOutputPipe : CompilerPipeline
{
    public override void Action()
    {
        if (!OutputDirectory.Exists)
            OutputDirectory.Create();
        else if (Target.HasChanged)
            OutputDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).ForEach(x => x.Delete());

        
        var wil_file = ObjectDirectory.File($"{Project.Name}.o");
        var dir = ObjectDirectory.SubDirectory("fragments").Ensure();

        dir.EnumerateFiles("*.*").ForEach(x => x.Delete());

        if (Target.HasChanged)
        {
            var wil_data = Module.BakeByteArray();

            Assembly = new IshtarAssembly(Module);

            IshtarAssembly.WriteTo(Assembly, OutputBinaryPath.FullName);

            File.WriteAllBytes(wil_file.FullName, wil_data);

            var dict = new ConcurrentDictionary<string, string>();
            Parallel.ForEachAsync(Module.class_table.OfType<IBaker>(),
                async (baker, token) => 
                {
                    if (baker is ClassBuilder clazz)
                    {
                        var g = Guid.NewGuid().ToString("N");
                        dict.TryAdd($"class:{clazz.Name.name}", g);
                        await dir.File($"{g}.o").WriteAllBytesAsync(baker.BakeByteArray(), token);
                        await dir.File($"{g}.symbol").WriteAllTextAsync(baker.BakeDebugString(), token);
                        await dir.File($"{g}.e").WriteAllTextAsync(baker.BakeDiagnosticDebugString(), token);
                    }
                }).Wait();
            ObjectDirectory.File("db.json").WriteAllText(JsonConvert.SerializeObject(dict));
            ObjectDirectory.File($"{Project.Name}.symbol.lay").WriteAllText(IshtarAssembly.GetDebugData(Assembly));
        }

        SaveArtifacts(new BinaryArtifact(OutputBinaryPath, Project.Name));
    }

    public override bool CanApply(CompileSettings flags) => true;
    public override int Order => 0;
}
