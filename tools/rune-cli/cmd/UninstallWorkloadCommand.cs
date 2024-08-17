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
    public const int PACKAGE_NOT_FOUND = -15;

    private static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");
    private readonly SymlinkCollector Symlink = new(SecurityStorage.RootFolder);

    public override async Task<int> ExecuteAsync(ProgressContext context, UninstallWorkloadCommandSettings settings)
    {
        using var task = context.AddTask($"delete [orange3]'{settings.PackageName}'[/] workload...")
            .IsIndeterminate();
        await Task.Delay(1000);
        var name = settings.PackageName;

        if (!WorkloadDirectory
                .SubDirectory(name).Exists)
        {
            task.FailTask();
            Log.Error($"Workload package [orange3]'{name}'[/] not installed.");
            return PACKAGE_NOT_FOUND;
        }

        var version = await WorkloadDirectory
            .SubDirectory(name).File("latest.version").ReadToEndAsync();
        var tagFolder = WorkloadDirectory
            .SubDirectory(name)
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
