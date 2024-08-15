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

if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.Create();

if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
{
    MarkupLine("Platform is not supported.");
    return -1;
}

var skipIntro = 
    Environment.GetEnvironmentVariable("VEINC_NOVID") is not null;

var watch = Stopwatch.StartNew();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    System.Console.OutputEncoding = Encoding.Unicode;
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


AppFlags.RegisterArgs(ref args);


var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<CompileCommand>("build")
        .WithDescription("Build current project.");

    config.SetExceptionHandler((ex) => {
        WriteException(ex);
    });
});

var result = app.Run(args);

watch.Stop();

return result;
