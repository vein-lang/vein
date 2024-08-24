using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using vein;
using vein.project;
using vein.runtime;

public class WorkspaceService(ILanguageServerFacade languageServer,
    ShardStorage shardStorage,
    LSPAssemblyResolver assemblyResolver)
{
    private InitializeParams @params;
    private FileInfo _projectFile;

    public List<VeinModule> Modules { get; private set; }

    public VeinProject CurrentProject() =>
        VeinProject.LoadFrom(_projectFile);

    public async Task Begin(InitializeParams p, CancellationToken ct = default)
    {
        @params = p;
        if (@params.RootUri is null)
            throw new ArgumentNullException(nameof(@params.RootUri));
        var directory = new DirectoryInfo(@params.RootUri.GetFileSystemPath());

        if (!directory.Exists)
            throw new NotSupportedException();

        var projects = directory.EnumerateFiles("*.vproj").ToList();

        if (projects.Count == 0)
        {
            languageServer.Window.ShowError("No any projects in root directory found...");
            throw new InvalidOperationException();
        }
        if (projects.Count > 1)
        {
            languageServer.Window.ShowError("Multiple projects in root directory found...");
            throw new InvalidOperationException();
        }

        _projectFile = projects.First();

        Modules = await GetDependencies();
    }


    public async Task<List<VeinModule>> GetDependencies()
    {
        if (_projectFile is null)
            return new List<VeinModule>();
        var project = CurrentProject();
        

        foreach (var package in project.Dependencies.Packages)
        {
            assemblyResolver.AddSearchPath(shardStorage.GetPackageSpace(package.Name, package.Version)
                .SubDirectory("lib"));
        }

        var list = new List<VeinModule>();
        foreach (var package in project.Dependencies.Packages)
        {
            try
            {
                list.Add(assemblyResolver.ResolveDep(package, list));
            }
            catch (Exception e)
            {
                languageServer.Window.ShowError("Failed to load dependencies. Try 'rune restore'");
                throw;
            }
        }

        return list;
    }
}
