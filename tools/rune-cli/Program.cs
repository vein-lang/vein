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
using Tomlyn.Extensions.Configuration;
using vein.cli;
using vein.services;
using Sentry.Profiling;

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
    options.AddIntegration(new ProfilingIntegration(
        TimeSpan.FromMilliseconds(10)
    ));
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
    (args.FirstOrDefault()?.Equals("sys") ?? false);

var watch = Stopwatch.StartNew();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.OutputEncoding = Encoding.Unicode;

ILGenerator.DoNotGenDebugInfo = false;

if (!skipIntro)
{
    MarkupLine($"[grey]Vein's Rune CLI[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
    MarkupLine($"[grey]Copyright (C)[/] [cyan3]2024[/] [bold]Vein[/].\n\n");
}

AppFlags.RegisterArgs(ref args);

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.None)).UseConsoleLifetime()
    .ConfigureAppConfiguration(x => x.AddTomlFile(ShardStorage.VeinRootFolder.File("rune.toml").FullName, true))
    .UseSpectreConsole(config => {
        config.AddCommand<RunCommand>("run")
            .WithDescription("Run project");
        config.AddCommand<TestCommand>("test")
            .WithDescription("Run test in project");
        config.AddCommand<NewCommand>("new")
            .WithDescription("Create new project.");
        config.AddCommand<BuildCommand>("build")
            .WithDescription("Build current project.");
        config.AddCommand<PackageCommand>("package")
            .WithDescription("Prepare and build project to publish into package registry.");
        config.AddCommand<CleanCommand>("clean")
            .WithDescription("Clean project cache");
        config.AddCommand<RestoreCommand>("restore")
            .WithDescription("Restore dependencies in project.");
        config.AddCommand<AddCommand>("add")
            .WithDescription("Find and add package into project from registry")
            .WithExample(["add std@0.12.1"]);
        config.AddCommand<PublishCommand>("publish")
            .WithDescription("Publish shard package into vein gallery. (need set 'packable: true' in project or call 'vein package')")
            .WithExample(["--project ./foo.vproj"]);
        config.AddCommand<TelemetryCommand>("telemetry")
            .IsHidden();
        config.AddBranch("workload", x =>
        {
            x.SetDescription("Manage vein workloads");
            x.AddCommand<ListInstalledWorkloadCommand>("list")
                .WithDescription($"Get list of installed workloads");
            x.AddCommand<InstallWorkloadCommand>("install")
                .WithDescription("Install workload into global");
            x.AddCommand<InstallWorkloadCommand>("add")
                .IsHidden()
                .WithDescription("Install workload into global");
            x.AddCommand<UpdateWorkloadCommand>("update")
                .WithDescription("Update workload.");
            x.AddCommand<UninstallWorkloadCommand>("uninstall")
                .WithDescription("Uninstall workload.");
            x.AddCommand<UninstallWorkloadCommand>("delete")
                .IsHidden()
                .WithDescription("Uninstall workload.");
            x.AddCommand<UninstallWorkloadCommand>("remove")
                .IsHidden()
                .WithDescription("Uninstall workload.");
        }).WithAlias("workloads");
        config.AddBranch("config", x =>
        {
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
        });

        config.AddBranch("sys", x =>
        {
            x.HideBranch();
            x.AddCommand<WorkloadGetTool>("get-tool")
                .IsHidden();
        });

        config.SetExceptionHandler((ex) => {
            SentrySdk.CaptureException(ex);

            if (Environment.GetEnvironmentVariable("RUNE_EXCEPTION_SHOW") is not null)
                WriteException(ex);
            File.WriteAllText($"rune-error-{DateTimeOffset.Now:yyyy-dd-M--HH-mm-ss}.txt", ex.ToString());
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

if (!skipIntro)
    MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");
return Environment.ExitCode;


