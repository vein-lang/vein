using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using vein;
using vein.cmd;
using static System.Console;
#pragma warning disable CS0436 // Type conflicts with imported type
using static GitVersionInformation;
#pragma warning restore CS0436 // Type conflicts with imported type
using static Spectre.Console.AnsiConsole;

[assembly: InternalsVisibleTo("veinc_test")]



if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
{
    MarkupLine("Platform is not supported.");
    return -1;
}
var watch = Stopwatch.StartNew();

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    OutputEncoding = Encoding.Unicode;
JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
{
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Culture = CultureInfo.InvariantCulture
};

var font = new FileInfo($"{AppDomain.CurrentDomain.BaseDirectory}/resources/isometric1.flf");
if (font.Exists)
{
    Render(new FigletText(FigletFont.Load(font.FullName), "Vein Lang")
            .LeftAligned()
            .Color(Color.Red));
}


MarkupLine($"[grey]Vein Lang Compiler[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
MarkupLine($"[grey]Copyright (C)[/] [cyan3]2021[/] [bold]Yuuki Wesp[/].\n\n");

ColorShim.Apply();

AppFlags.RegisterArgs(ref args);




var app = new CommandApp();



app.Configure(config => config.AddCommand<CompileCommand>("build"));

var result = app.Run(args);

watch.Stop();

MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");

return result;
