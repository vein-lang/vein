using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using ishtar.emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectre.Console;
using Spectre.Console.Cli;
using vein;
using vein.cmd;
using static Spectre.Console.AnsiConsole;
using vein.json;
using vein.resources;
using static vein.GlobalVersion;
[assembly: InternalsVisibleTo("veinc_test")]

if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null)
    AnsiConsole.Console = RawConsole.Create();

if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
{
    MarkupLine("Platform is not supported.");
    return -1;
}

var skipIntro = SecurityStorage.HasKey("app:novid") ||
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

ColorShim.Apply();
ILGenerator.DoNotGenDebugInfo = false;

if (!skipIntro)
{
    var font = Resources.Font;
    if (font.Exists)
    {
        Write(new FigletText(FigletFont.Load(font.FullName), "Vein Lang")
            .LeftJustified()
            .Color(Color.Red));
    }
}


if (!skipIntro)
{
    MarkupLine($"[grey]Vein Lang Compiler[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
    MarkupLine($"[grey]Copyright (C)[/] [cyan3]2024[/] [bold]Yuuki Wesp[/].\n\n");
}



AppFlags.RegisterArgs(ref args);


var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<NewCommand>("new")
        .WithDescription("Create new project.");
    config.AddCommand<CompileCommand>("build")
        .WithDescription("Build current project.");
    config.AddCommand<PackageCommand>("package")
        .WithDescription("Prepare and build project to publish into package registry.");
    config.AddCommand<CleanCommand>("clean")
        .WithDescription("Clean project cache");
    config.AddCommand<RestoreCommand>("restore")
        .WithDescription("Restore dependencies in project.");
    config.AddCommand<AddCommand>("add")
        .WithDescription("Find and add package into project from registry")
        .WithExample(new string[] { "add std@0.12.1" });
    config.AddCommand<PublishCommand>("publish")
        .WithDescription("Publish shard package into vein gallery. (need set 'packable: true' in project or call 'veinc package')")
        .WithExample(new string[] { "--project ./foo.vproj" });
    config.AddBranch("config", x =>
    {
        x.AddCommand<SetConfigCommand>("set")
            .WithExample(new string[] { "set foo:bar value" })
            .WithExample(new string[] { "set foo:zoo 'a sample value'" })
            .WithDescription("Set value config by key in global storage.");
        x.AddCommand<GetConfigCommand>("get")
            .WithExample(new string[1] { "get foo:bar" })
            .WithDescription("Get value config by key from global storage.");
        x.AddCommand<ListConfigCommand>("list")
            .WithDescription("Get all keys.");
        x.AddCommand<RemoveConfigCommand>("remove")
            .WithDescription("Remove key from global config.");
    });
});

var result = app.Run(args);

watch.Stop();

MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");

return result;
