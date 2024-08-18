namespace vein.cmd;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using compilation;
using project;
using Spectre.Console;
using Spectre.Console.Cli;

public class RestoreCommand : AsyncCommandWithProject<RestoreCommandSettings>
{
    public ProgressContext ProgressContext { get; set; }
    public static readonly Uri VEIN_GALLERY = new("https://api.vein-lang.org/");
    public ShardRegistryQuery query = new ShardRegistryQuery(VEIN_GALLERY)
        .WithStorage(new ShardStorage());
    public override async Task<int> ExecuteAsync(CommandContext context, RestoreCommandSettings settings,
        VeinProject project)
    {
        using var tag = ScopeMetric.Begin("project.restore")
            .WithProject(project);
        if (ProgressContext == null)
            return await AnsiConsole
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

        var task = ProgressContext.AddTask($"Restore [orange]'{project.Name}'[/] project.");
        var graph = await CollectDependencyGraphAsync(project, task);
        var failed = false;
        task.IsIndeterminate(false);
        task.MaxValue(graph.Count);
        await Parallel.ForEachAsync(graph, async (target, token) =>
        {
            try
            {
                await query.DownloadShardAsync(target, token);
            }
            catch (Exception e)
            {
                failed = true;
                Log.Error($"[red]Failed[/] sync [orange]'{target.Name}@{target.Version}'[/] shard.");
                Log.Error($"{e.Message}");
                throw;
            }
            task.VeinStatus($"Shard package [orange]'{target.Name}@{target.Version}'[/] has been synced.");
            task.Increment(1);
        });

        if (failed)
        {
            Log.Error($"[red]Failed[/] sync [orange]'{project.Name}'[/] project.");
            return -1;
        }
        Log.Info($"[green]Success[/] sync [orange]'{project.Name}'[/] project.");
        return 0;
    }

    private async Task<List<RegistryPackage>> CollectDependencyGraphAsync(VeinProject project, ProgressTask task)
    {
        task.IsIndeterminate(true);
        var list = new List<RegistryPackage>();

        foreach (var dependency in project.Dependencies.Packages.ToList())
            await FetchAsync(list, task, dependency);

        return list;
    }

    private async Task FetchAsync(List<RegistryPackage> container, ProgressTask task, PackageReference @ref)
    {
        task.VeinStatus($"Fetch '{@ref.Name}@{@ref.Version}'...");
        var q = await query.FindByName(@ref.Name, $"{@ref.Version}");

        if (q is null)
            throw new PackageNotFoundException($"{@ref.Name}");

        foreach (var dependency in q.Dependencies)
            await FetchAsync(container, task, dependency);
        container.Add(q);
    }
}

public class PackageNotFoundException(string msg) : Exception(msg);

public class RestoreCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to project")]
    [CommandOption("--project")]
    public string Project { get; set; }
}
