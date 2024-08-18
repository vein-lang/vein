namespace vein.cmd;

using System.ComponentModel;
using Newtonsoft.Json;
using project;
using Spectre.Console;
using Spectre.Console.Cli;
using styles;

[ExcludeFromCodeCoverage]
public class UninstallWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required string PackageName { get; init; }
}
public class UninstallWorkloadCommand : AsyncCommandWithProgress<UninstallWorkloadCommandSettings>
{
    private static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");
    private readonly SymlinkCollector Symlink = new(SecurityStorage.RootFolder);

    public override async Task<int> ExecuteAsync(ProgressContext context, UninstallWorkloadCommandSettings settings)
    {
        using var tag = ScopeMetric.Begin("workload.uninstall")
            .WithWorkload(settings.PackageName, "");
        using var task = context.AddTask($"delete [orange3]'{settings.PackageName}'[/] workload...")
            .IsIndeterminate();
        await Task.Delay(1000);
        var package = settings.PackageName;

        if (!WorkloadDirectory
                .SubDirectory(package).Exists)
        {
            task.FailTask();
            Log.Error($"Workload package [orange3]'{package}'[/] not installed.");
            return -1;
        }

        var version = await WorkloadDirectory
            .SubDirectory(package).File("latest.version").ReadToEndAsync();
        var tagFolder = WorkloadDirectory
            .SubDirectory(package)
            .SubDirectory(version);

        var manifest = await WorkloadManifest.OpenAsync(tagFolder.File("workload.manifest.json"));


        foreach (var (key, pkg) in manifest.Packages)
        {
            foreach (var @base in pkg.Definition)
            {
                if (@base is WorkloadPackageTool tool)
                {
                    var file = new FileInfo(Symlink.ToExec(tool.ExecPath));
                    Symlink.DeleteSymlink(string.IsNullOrEmpty(tool.OverrideName) ?
                        Path.GetFileNameWithoutExtension(file.Name) :
                        tool.OverrideName);
                }
                else
                    throw new NotSupportedException();
            }
        }

        tagFolder.Delete(true);

        return 0;
    }
}
