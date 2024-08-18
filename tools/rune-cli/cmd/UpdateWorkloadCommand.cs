namespace vein.cmd;

using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using static UninstallWorkloadCommand;

[ExcludeFromCodeCoverage]
public class UpdateWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required RunePackageKey PackageName { get; set; }

    [Description("Package version")]
    [CommandOption("--manifest", IsHidden = true)]
    public string? ManifestFile { get; set; }
}

public class UpdateWorkloadCommand(ShardRegistryQuery query, ShardStorage storage, WorkloadDb db) : AsyncCommandWithProgress<UpdateWorkloadCommandSettings>
{
    public static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");

    public override async Task<int> ExecuteAsync(ProgressContext context, UpdateWorkloadCommandSettings settings)
    {
        var packageName = settings.PackageName.Name;
        var packageVersion = settings.PackageName.Version;
        using var tag = ScopeMetric.Begin("workload.update")
            .WithWorkload(packageName, packageVersion);
        if (!WorkloadDirectory.SubDirectory(packageName).Exists)
        {
            var uninstallResult = await new UninstallWorkloadCommand().ExecuteAsync(context,
                new UninstallWorkloadCommandSettings()
                {
                    PackageName = settings.PackageName.Name
                });
            if (uninstallResult is not 0 )
                return uninstallResult;
        }
        var installResult = await new InstallWorkloadCommand(query, storage, db).ExecuteAsync(null,
            new InstallWorkloadCommandSettings()
            {
                PackageName = settings.PackageName,
                ManifestFile = settings.ManifestFile
            });
        return installResult;
    }
}

