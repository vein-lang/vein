using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
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
