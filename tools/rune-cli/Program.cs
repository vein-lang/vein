using ishtar.emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NuGet.Versioning;
using vein;
using vein.cmd;
using vein.json;
using vein.project;
using static Spectre.Console.AnsiConsole;
using static vein.GlobalVersion;
[assembly: InternalsVisibleTo("veinc_test")]


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

ILGenerator.DoNotGenDebugInfo = false;

if (!skipIntro)
{
    MarkupLine($"[grey]Vein's Rune CLI[/] [red]{AssemblySemFileVer}-{BranchName}+{ShortSha}[/]");
    MarkupLine($"[grey]Copyright (C)[/] [cyan3]2024[/] [bold]Yuuki Wesp[/].\n\n");
}

AppFlags.RegisterArgs(ref args);

var app = new CommandApp();

app.Configure(config =>
{
    config.Settings.ApplicationVersion
        = $"Vein Rune CLI {AssemblySemFileVer}\nBranch: {BranchName}+{ShortSha}\nCall rune workloads list for view installed workloads and other version";
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
        .WithExample(["add std@0.12.1"]);
    config.AddCommand<PublishCommand>("publish")
        .WithDescription("Publish shard package into vein gallery. (need set 'packable: true' in project or call 'vein package')")
        .WithExample(["--project ./foo.vproj"]);
    config.AddBranch("workload", x =>
    {
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
    });
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

    config.SetExceptionHandler((ex) => {
#if DEBUG
        WriteException(ex);
#else
        File.WriteAllText($"rune-error-{DateTimeOffset.Now:s}.txt", ex.ToString());
#endif
    });
});

var result = app.Run(args);

watch.Stop();

MarkupLine($":sparkles: Done in [lime]{watch.Elapsed.TotalSeconds:00.000}s[/].");

return result;
