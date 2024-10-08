namespace vein.cmd;

using System.Collections.Generic;

[ExcludeFromCodeCoverage]
public class AddCommand(ShardRegistryQuery query) : AsyncCommandWithProject<AddCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddCommandSettings settings, VeinProject project)
    {
        var name = settings.PackageName.Name;
        var version = settings.PackageName.Version;

        
        var result = await ProgressWithTask.ProgressIndeterminate(async () =>
                await query.FindByName(name, version, settings.IncludeUnlisted), $"search [orange3]'{name}@{version}'[/] package in package registry...");

        if (result is null)
        {
            Log.Error($"Shard package [orange3]'{name}@{version}'[/] not found in vein gallery.");
            return -1;
        }
        var package_tag = $"{name}@{result.Version.ToNormalizedString()}";
        project._project.Packages ??= new List<string>();

        if (project._project.Packages.Contains(package_tag))
        {
            Log.Info($"Shard package [orange3]'{name}@{result.Version.ToNormalizedString()}'[/] already installed.");
            return 0;
        }
        await ProgressWithTask.Progress(
            (x) => query.DownloadShardAsync(result, CancellationToken.None, x),
            $"downloading shard package [orange3]'{name}@{result.Version.ToNormalizedString()}'[/] {{%bytes}}");

        
        

        // remove old versions of package
        if (project._project.Packages.Any(x => x.StartsWith($"{name}@")))
        {
            var el = (project._project.Packages.First(x => x.StartsWith($"{name}@")));
            project._project.Packages.Remove(el);
        }

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
            Log.Error($"[red]Failed[/] add [orange3]'{name}@{result.Version.ToNormalizedString()}'[/] into [orange3]'{project.Name}'[/] project.");
            project._project.Packages.Remove(package_tag);
            project._project.Save(project.ProjectFile);
            return -1;
        }
        Log.Info($"[green]Success[/] add [orange3]'{name}@{result.Version.ToNormalizedString()}'[/] into [orange3]'{project.Name}'[/] project.");
        return 0;
    }
}


public class AddCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Package name")]
    [CommandArgument(0, "[NAME]")]
    public required RunePackageKey PackageName { get; set; }

    [Description("Path to project")]
    [CommandOption("--project")]
    public string Project { get; set; }

    [Description("Include unlisted package")]
    [CommandOption("--unlisted")]
    public bool IncludeUnlisted { get; set; }
}


public class ShardProxy(ShardRegistryQuery query)
{
    public Task<int> Install(RunePackageKey key, VeinProject project) =>
        new AddCommand(query).ExecuteAsync(null, new AddCommandSettings()
        {
            PackageName = key,
            Project = project.ProjectFile.FullName
        }, project);
}
