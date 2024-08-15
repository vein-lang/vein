namespace vein.pipes;

using compilation;
using fs;
using project;

[ExcludeFromCodeCoverage]
public abstract class CompilerPipeline
{
    protected DirectoryInfo OutputDirectory
        => new(Path.Combine(Project.WorkDir.FullName, "bin"));
    protected DirectoryInfo ObjectDirectory
        => new(Path.Combine(Project.WorkDir.FullName, "obj"));
    protected FileInfo OutputBinaryPath =>
        new(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wll"));

    protected internal VeinModuleBuilder Module { get; set; }
    protected internal VeinProject Project { get; set; }
    protected internal IshtarAssembly Assembly { get; set; }
    protected internal CompilationTarget Target { get; set; }

    public abstract void Action();

    public Action<VeinArtifact> PopulateArtifact;

    public IList<VeinArtifact> Artifacts = new List<VeinArtifact>();

    public void SaveArtifacts(VeinArtifact artifact)
    {
        Artifacts.Add(artifact);
        PopulateArtifact(artifact);
    }

    public abstract bool CanApply(CompileSettings flags);
    public abstract int Order { get; }
}
