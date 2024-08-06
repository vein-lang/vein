namespace vein.cmd;

using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

[ExcludeFromCodeCoverage]
public class UpdateWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required string PackageName { get; set; }

    [Description("Package version")]
    [CommandOption("--version")]
    public string? PackageVersion { get; set; }

    [Description("Package version")]
    [CommandOption("--manifest", IsHidden = true)]
    public string? ManifestFile { get; set; }
}

public class UpdateWorkloadCommand : AsyncCommandWithProgress<UpdateWorkloadCommandSettings>
{
    public static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");
    public static readonly Uri VEIN_GALLERY = new("https://api.vein-lang.org/");
    public static readonly ShardStorage Storage = new();

    public override async Task<int> ExecuteAsync(ProgressContext context, UpdateWorkloadCommandSettings settings)
    {
        var uninstallResult = await new UninstallWorkloadCommand().ExecuteAsync(context,
            new UninstallWorkloadCommandSettings()
            {
                PackageName = settings.PackageName
            });
        if (uninstallResult != 0)
            return uninstallResult;
        var installResult = await new InstallWorkloadCommand().ExecuteAsync(context,
            new InstallWorkloadCommandSettings()
            {
                PackageName = settings.PackageName,
                ManifestFile = settings.ManifestFile,
                PackageVersion = settings.PackageVersion
            });
        return installResult;
    }
}

