using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using lsp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using Serilog;
using Spectre.Console.Cli;
using vein;
using vein.cli;
using static Spectre.Console.AnsiConsole;
using vein.json;
using Log = Serilog.Log;

[assembly: InternalsVisibleTo("veinc_test")]

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.Create();
else if (Environment.GetEnvironmentVariable("FORK_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.CreateForkConsole();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.OutputEncoding = Encoding.Unicode;

var redirectLogger = Environment.GetEnvironmentVariable("LOG_WRITE_TO_FILE");


// for lsp
if (!string.IsNullOrEmpty(redirectLogger) && new DirectoryInfo(redirectLogger).Exists)
{
    var logFile = new DirectoryInfo(redirectLogger).Ensure().File("vein.lsp.log");
    Serilog.Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.File(logFile.FullName)
        .MinimumLevel.Verbose()
        .CreateLogger();
}
else
{
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .MinimumLevel.Verbose()
        .CreateLogger();
}


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
        config.AddCommand<LspRunCommand>("lsp")
            .WithDescription("run lsp server");

        config.SetExceptionHandler((ex) => {
            if (!string.IsNullOrEmpty(redirectLogger))
                Log.Logger.Error(ex, "");
            WriteException(ex);
        });
    })
    .ConfigureServices(
        (_, services) =>
        {
        })
    .RunConsoleAsync();
return Environment.ExitCode;
