namespace vein.cmd;

using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using static UninstallWorkloadCommand;

[ExcludeFromCodeCoverage]
public class UpdateWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required string PackageName { get; set; }

    [Description("Package version")]
    [CommandOption("--manifest", IsHidden = true)]
    public string? ManifestFile { get; set; }
}

public class UpdateWorkloadCommand(ShardRegistryQuery query, ShardStorage storage, WorkloadDb db) : AsyncCommandWithProgress<UpdateWorkloadCommandSettings>
{
    public static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");

    public override async Task<int> ExecuteAsync(ProgressContext context, UpdateWorkloadCommandSettings settings)
    {
        var uninstallResult = await new UninstallWorkloadCommand().ExecuteAsync(context,
            new UninstallWorkloadCommandSettings()
            {
                PackageName = settings.PackageName.Split("@").First()
            });
        if (uninstallResult is not 0 and PACKAGE_NOT_FOUND)
            return uninstallResult;
        var installResult = await new InstallWorkloadCommand(query, storage, db).ExecuteAsync(context,
            new InstallWorkloadCommandSettings()
            {
                PackageName = settings.PackageName,
                ManifestFile = settings.ManifestFile
            });
        return installResult;
    }
}

