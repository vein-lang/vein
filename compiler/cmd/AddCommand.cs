namespace vein.cmd;

using System;
using System.Collections.Generic;
using project;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;

[ExcludeFromCodeCoverage]
public class AddCommand : AsyncCommandWithProject<AddCommandSettings>
{
    public static readonly Uri VEIN_GALLERY = new Uri("https://api.vein.gallery/");
    public override async Task<int> ExecuteAsync(CommandContext context, AddCommandSettings settings, VeinProject project)
    {
        var name = settings.PackageName ?? throw new ArgumentNullException(nameof(settings.PackageName));
        var version = settings.PackageVersion ?? "latest";
        
        project._project.Packages ??= new List<string>();
        
        var query = new ShardRegistryQuery(VEIN_GALLERY)
            .WithStorage(new ShardStorage());
        
        var result = await query.DownloadShardAsync(name, version);
        
        if (result is null)
        {
            Log.Error($"Shard package [orange]'{name}@{version}'[/] not found in vein gallery.");
            return -1;
        }

        var package_tag = $"{name}@{result.Version.ToNormalizedString()}";

        if (project._project.Packages.Contains(package_tag))
            return 0;

        project._project.Packages.Add(package_tag);
        project._project.Save(project.ProjectFile);

        var exit_code = await AnsiConsole
            .Progress()
            .AutoClear(false)
            .AutoRefresh(true)
            .HideCompleted(true)
            .Columns(
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn { Spinner = Spinner.Known.Dots8Bit, CompletedText = "✅", FailedText = "❌" },
                new TaskDescriptionColumn { Alignment = Justify.Left })
            .StartAsync(async ctx => await new RestoreCommand() { ProgressContext = ctx }
                .ExecuteAsync(context, new RestoreCommandSettings() { Project = settings.Project }, project));

        if (exit_code != 0)
        {
            Log.Error($"[red]Failed[/] add [orange]'{name}@{version}'[/] into [orange]'{project.Name}'[/] project.");
            project._project.Packages.Remove(package_tag);
            project._project.Save(project.ProjectFile);
            return -1;
        }
        Log.Info($"[green]Success[/] add [orange]'{name}@{version}'[/] into [orange]'{project.Name}'[/] project.");
        return 0;
    }
}


public class AddCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Package name")]
    [CommandArgument(0, "[NAME]")]
    public string PackageName { get; set; }
    [Description("Path to project")]
    [CommandOption("--project")]
    public string Project { get; set; }
    [Description("Package version")]
    [CommandOption("--version")]
    public string PackageVersion { get; set; }
}