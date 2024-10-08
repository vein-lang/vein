namespace vein.compilation;

using fs;
using project;

public record CompilationTarget
{
    private CompilationStatus _status { get; set; } = CompilationStatus.NotStarted;

    public VeinProject Project { get; }

    public CompilationStatus Status
    {
        get => _status;
        set
        {
            if (value == CompilationStatus.Failed)
                Task.FailTask();
            if (value == CompilationStatus.Success)
            {
                Task.MaxValue = 10;
                Task.Value(10);
                Task.SuccessTask();
            }
            this._status = value;
        }
    }

    public CompilationTarget This() => this;
    public CompilationLog Logs { get; } = new();
    public List<CompilationTarget> Dependencies { get; } = new();
    public IProgressionTask Task { get; set; }
    public IReadOnlyCollection<VeinArtifact> Artifacts { get; private set; } = new List<VeinArtifact>();
    public HashSet<VeinModule> LoadedModules { get; } = new();
    public AssemblyResolver Resolver { get; }
    public CompilationTarget(VeinProject p, IProgressionTask ctx)
        => (Project, Task, Resolver) =
            (p, ctx.AddChildTask($"[red](waiting)[/] Compile [orange]'{p.Name}'[/]...", allowHide: false)
                .WithState("project", p), new(this));


    // Indicate files has changed
    public bool HasChanged { get; set; }

    public Dictionary<FileInfo, DocumentDeclaration> AST { get; } = new();
    public CompileSettings CompilationSettings { get; internal set; }

    public DirectoryInfo GetOutputDirectory()
        => new(Path.Combine(Project.WorkDir.FullName, "bin"));

    public CompilationTarget AcceptArtifacts(IReadOnlyCollection<VeinArtifact> artifacts)
    {
        Artifacts = artifacts;
        foreach (var artifact in artifacts)
            Log.Info($"Populated artifact with [purple]'{artifact.Kind}'[/] type, path: [gray]'{artifact.Path}'[/]", this);
        return this;
    }
}
