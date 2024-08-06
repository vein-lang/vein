namespace vein.cmd;

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using project;
using Spectre.Console;
using Spectre.Console.Cli;

public class PublishCommand : AsyncCommandWithProject<PublishCommandSettings>
{
    public static readonly Uri VEIN_GALLERY = new Uri("https://api.vein-lang.org/");

    public override async Task<int> ExecuteAsync(CommandContext context, PublishCommandSettings settings,
        VeinProject project)
    {
        var store = new ShardStorage();
        var query = new ShardRegistryQuery(VEIN_GALLERY)
            .WithStorage(store);
        var name = project.Name;
        var version = project.Version;
        var pkg = store.TemplateName(project.Name, project.Version);

        var file = project.WorkDir.SubDirectory("bin").File(pkg);

        if (!file.Exists)
        {
            Log.Error($"Shard package [orange3]'{pkg}'[/] not found in binary folder, maybe need build before publish?");
            return -1;
        }

        var apiKey = settings.ApiKey ?? SecurityStorage.GetByKey<string>("registry:api:token");

        if (apiKey is null)
        {
            Log.Error($"Api key is not [red]provided[/], set api key with parameter '--api-key' " +
                      $"or set with config 'rune config set registry:api:token {Guid.Empty}'");
            return -1;
        }

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Pipe)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync("publish...",
                async ctx => await query.WithApiKey(apiKey).PublishPackage(file));

        switch (result)
        {
            case (_, 201):
                Log.Info($"[green]Success[/] publish [orange3]'{name}@{version}'[/] into [orange3]package registry[/].");
                return 0;
            case (var r, var status):
                Log.Error($"[red]Failed[/] publish [orange3]'{name}@{version}'[/] into registry.");
                Log.Error($"[red]Response[/]: ({status}) [orange3]'{r.message}'[/].");
                return -1;
        }
    }
}


public class PublishCommandSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to project")]
    [CommandOption("--project")]
    public required string Project { get; set; }
    [Description("API Key for publishing")]
    [CommandOption("--api-key")]
    public string? ApiKey { get; set; }
}
