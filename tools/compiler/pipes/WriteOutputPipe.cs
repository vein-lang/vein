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


        var wil_file = new FileInfo(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wll.bin"));


        if (Target.HasChanged)
        {
            var wil_data = Module.BakeByteArray();


            Assembly = new IshtarAssembly(Module);

            IshtarAssembly.WriteTo(Assembly, OutputBinaryPath.FullName);

            File.WriteAllBytes(wil_file.FullName, wil_data);
        }

        SaveArtifacts(new ILArtifact(wil_file, Project.Name));
        SaveArtifacts(new BinaryArtifact(OutputBinaryPath, Project.Name));
        SaveArtifacts(new DebugSymbolArtifact(new FileInfo($"{wil_file.FullName}.lay"), Project.Name));
    }

    public override bool CanApply(CompileSettings flags) => true;
    public override int Order => 0;
}
