using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Sentry.Infrastructure;
using Sentry.Profiling;
using Spectre.Console.Cli;
using vein;
using vein.cli;
using static Spectre.Console.AnsiConsole;
using vein.json;
[assembly: InternalsVisibleTo("veinc_test")]

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.Create();
else if (Environment.GetEnvironmentVariable("FORK_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.CreateForkConsole();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.OutputEncoding = Encoding.Unicode;

using var _ = SentrySdk.Init(options => {
    options.Dsn = "https://e3bce714623baf7826ff918cbd1795d8@o958881.ingest.us.sentry.io/4507797542141952";
    options.Debug = true;
    options.AutoSessionTracking = true;
    options.TracesSampleRate = 1.0;
    options.ProfilesSampleRate = 0.5;
    options.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
    options.AddIntegration(new ProfilingIntegration(
        TimeSpan.FromMilliseconds(10)
    ));
    options.ExperimentalMetrics = new ExperimentalMetricsOptions
    {
        EnableCodeLocations = true
    };
});
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("app.ver", GlobalVersion.AssemblySemFileVer);
    scope.SetTag("app.branch", GlobalVersion.BranchName);
    scope.SetTag("app.sha", GlobalVersion.ShortSha);
});
var watch = Stopwatch.StartNew();

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Culture = CultureInfo.InvariantCulture,
    Converters = new List<JsonConverter>()
    {
        new FileInfoSerializer(),
        new StringEnumConverter()
    }
};

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.None))
    .UseConsoleLifetime()
    .UseSpectreConsole(config =>
    {
        config.AddCommand<CompileCommand>("build")
            .WithDescription("Build current project.");

        config.SetExceptionHandler((ex) => {
            WriteException(ex);
            SentrySdk.CaptureException(ex);
        });
    })
    .ConfigureServices(
        (_, services) =>
        {
        })
    .RunConsoleAsync();

watch.Stop();
MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");
return Environment.ExitCode;
