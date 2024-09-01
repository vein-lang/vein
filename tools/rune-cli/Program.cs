using ishtar.emit;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using vein;
using vein.cmd;
using vein.json;
using static Spectre.Console.AnsiConsole;
using static vein.GlobalVersion;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Infrastructure;
using vein.cli;
using vein.services;

[assembly: InternalsVisibleTo("veinc_test")]

await AppMutex.Begin();
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Culture = CultureInfo.InvariantCulture,
    Converters = new List<JsonConverter>()
    {
        new FileInfoSerializer(),
        new StringEnumConverter(),
        new WorkloadKeyContactConverter(),
        new PackageKeyContactConverter(),
        new WorkloadPackageBaseConverter(),
        new PlatformKeyContactConverter(),
        new PackageKindKeyContactConverter(),
        new WorkloadConverter(),
        new DictionaryAliasesConverter()
    }
};

using var sentry = SentrySdk.Init(options => {
    options.Dsn = "https://a035cc18b8bbf591aeaddd3a27fb5434@o958881.ingest.us.sentry.io/4507797531721728";
    options.Debug = true;
    options.AutoSessionTracking = true;
    options.TracesSampleRate = 1.0;
    options.ProfilesSampleRate = 1.0;
    options.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
    options.ExperimentalMetrics = new ExperimentalMetricsOptions
    {
        EnableCodeLocations = true
    };
});
SentrySdk.ConfigureScope(scope => {
    scope.SetTag("app.ver", AssemblySemFileVer);
    scope.SetTag("app.branch", BranchName);
    scope.SetTag("app.sha", ShortSha);
});
if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.Create();

var skipIntro =
    SecurityStorage.HasKey("app:novid") || // skip intro when setting is set
    Environment.GetEnvironmentVariable("RUNE_NOVID") is not null || // skip intro when using 'RUNE_NOVID' env
    (args.FirstOrDefault()?.Equals("run") ?? false) || // skip intro when command is 'run'
    (args.FirstOrDefault()?.Equals("sys") ?? false) ||
    (args.FirstOrDefault()?.Equals("--version") ?? false);

var watch = Stopwatch.StartNew();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.OutputEncoding = Encoding.Unicode;

ILGenerator.DoNotGenDebugInfo = false;

if (!skipIntro)
{
    MarkupLine($"[grey]Vein's Rune CLI[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
    MarkupLine($"[grey]Copyright (C)[/] [cyan3]2024[/] [bold]Vein[/].\n");
}

AppFlags.RegisterArgs(ref args);

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.None))
    .UseConsoleLifetime()
    .UseSpectreConsole(config =>
    {
        config.SetApplicationCulture(CultureInfo.InvariantCulture);
        config.SetApplicationName("rune-cli");
        config.SetApplicationVersion($"{AssemblySemFileVer}-{BranchName}+{ShortSha}");
        
        config.AddCommand<RunCommand>("run")
            .WithDescription("Run project")
            .WithAlias("start");
        config.AddCommand<TestCommand>("test")
            .WithDescription("Run test in project");
        config.AddCommand<NewCommand>("new")
            .WithDescription("Create new project.")
            .WithAlias("create");
        config.AddCommand<BuildCommand>("build")
            .WithDescription("Build current project.");
        config.AddCommand<PackageCommand>("package")
            .WithDescription("Prepare and build project to publish into package registry.")
            .WithAlias("pack");
        config.AddCommand<CleanCommand>("clean")
            .WithDescription("Clean project cache")
            .WithAlias("clear")
            .WithAlias("prune");
        config.AddCommand<RestoreCommand>("restore")
            .WithDescription("Restore dependencies in project.");
        config.AddCommand<AddCommand>("add")
            .WithDescription("Find and add package into project from registry")
            .WithExample(["add std@0.12.1"])
            .WithAlias("install")
            // ReSharper disable once StringLiteralTypo
            .WithAlias("instal");
        config.AddCommand<PublishCommand>("publish")
            .WithDescription("Publish shard package into vein gallery.")
            .WithExample(["--project ./foo.vproj"]);
        config.AddCommand<TelemetryCommand>("telemetry")
            .IsHidden();
        config.AddBranch("workload", x =>
        {
            x.SetDescription("Manage vein workloads");
            x.AddCommand<ListInstalledWorkloadCommand>("list")
                .WithDescription($"Get list of installed workloads");
            x.AddCommand<InstallWorkloadCommand>("install")
                .WithDescription("Install workload into global")
                .WithAlias("add")
                // ReSharper disable once StringLiteralTypo
                .WithAlias("instal");
            x.AddCommand<UpdateWorkloadCommand>("update")
                .WithDescription("Update workload.")
                .WithAlias("upgrade");
            x.AddCommand<UninstallWorkloadCommand>("uninstall")
                .WithDescription("Uninstall workload.")
                .WithAlias("remove")
                .WithAlias("delete");
        }).WithAlias("workloads");
        config.AddBranch("config", x =>
        {
            x.SetDescription("Manage vein configurations");
            x.AddCommand<SetConfigCommand>("set")
                .WithExample(["set foo:bar value"])
                .WithExample(["set foo:zoo 'a sample value'"])
                .WithDescription("Set value config by key in global storage.");
            x.AddCommand<GetConfigCommand>("get")
                .WithExample(["get foo:bar"])
                .WithDescription("Get value config by key from global storage.");
            x.AddCommand<ListConfigCommand>("list")
                .WithDescription("Get all keys.");
            x.AddCommand<RemoveConfigCommand>("remove")
                .WithDescription("Remove key from global config.");
        })
        .WithAlias("cfg");

        config.AddBranch("sys", x =>
        {
            x.HideBranch();
            x.AddCommand<WorkloadGetTool>("get-tool")
                .IsHidden();
        });

        config.SetExceptionHandler((ex, resolver) =>
        {
            if (ex is CommandParseException exc)
            {
                MarkupLine($"[red]{exc.Message}[/]");
                return;
            }

            MarkupLine($"([orange]{ex.GetType().FullName!.Split('.').Last().ToLowerInvariant().Replace("exception", "")}[/]) [red]{ex.Message}[/]");

            SentrySdk.CaptureException(ex);

            if (Environment.GetEnvironmentVariable("RUNE_EXCEPTION_SHOW") is not null)
                WriteException(ex);
            if (Directory.Exists("./obj"))
                File.WriteAllText($"./obj/rune-error-{DateTimeOffset.Now:yyyy-dd-M--HH-mm-ss}.txt", ex.ToString());
        });
    })
    .ConfigureServices(
        (ctx, services) =>
        {
            services.AddSingleton<ShardStorage>();
            services.AddScoped((x) => new ShardRegistryQuery(
                    new(ctx.Configuration.GetValue("galleryUrl", "https://api.vein-lang.org/")!))
                .WithStorage(x.GetRequiredService<ShardStorage>()));
            services.AddSingleton<WorkloadDb>();
            services.AddSingleton<ShardProxy>();
        })
    .RunConsoleAsync();

watch.Stop();
await AppMutex.End();

if (Environment.ExitCode == 0) if (!skipIntro)
    MarkupLine($"\n:sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");

return Environment.ExitCode;


