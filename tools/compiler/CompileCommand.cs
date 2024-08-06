namespace vein.cmd;

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using compilation;
using project;
using Spectre.Console;
using Spectre.Console.Cli;
using static Spectre.Console.AnsiConsole;

[ExcludeFromCodeCoverage]
public class CompileSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }

    [Description("Display exported types table")]
    [CommandOption("--print-result-types")]
    public bool PrintResultType { get; set; }

    [Description("Display exported types table")]
    [CommandOption("--disable-optimization|-O")]
    public bool DisableOptimization { get; set; }

    [Description("Compile into single file")]
    [CommandOption("--single-file|-s")]
    public bool HasSingleFile { get; set; }

    [Description("Wait to attach debbugger (ONLY DEBUG COMPILER)")]
    [CommandOption("--sys-debugger")]
    public bool IsNeedDebuggerAttach { get; set; }
    [Description("Enable stacktrace printing when error.")]
    [CommandOption("--sys-stack-trace")]
    public bool DisplayStacktraceGenerator { get; set; }

    [Description("Generate shard package.")]
    [CommandOption("--gen-shard")]
    public bool GeneratePackageOutput { get; set; }

    [Description("Ignore cache.")]
    [CommandOption("--ignore-cache")]
    public bool IgnoreCache { get; set; }
}


[ExcludeFromCodeCoverage]
public class CompileCommand : AsyncCommandWithProject<CompileSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, CompileSettings settings, VeinProject project)
    {
        Log.Info($"Project [orange]'{project.Name}'[/].");

        var targets = await CompilationTask.RunAsync(project.WorkDir, settings);

        foreach (var info in targets.SelectMany(x => x.Logs.Info))
            MarkupLine(info.TrimEnd('\n'));
        foreach (var info in Log.State.infos.Reverse())
            MarkupLine(info.Markup().TrimEnd('\n'));

        if (new[] { Log.State.errors.Count, targets.Sum(x => x.Logs.Error.Count) }.Sum() > 0)
        {
            var rule1 = new Rule($"[yellow]{Log.State.errors.Count} error found[/]") {Style = Style.Parse("red rapidblink")};
            Write(rule1);
        }

        foreach (var target in targets.SelectMany(x => x.Logs.Error))
            MarkupLine(target);

        foreach (var error in Log.State.errors.Reverse())
        {
            MarkupLine(error.Markup());
#if DEBUG
            WriteException(error.DebugStackTrace);
#endif
        }

        if (new[] { Log.State.warnings.Count, targets.Sum(x => x.Logs.Warn.Count) }.Sum() > 0)
        {
            var rule2 = new Rule($"[yellow]{Log.State.warnings.Count} warning found[/]") {Style = Style.Parse("orange rapidblink")};
            Write(rule2);
        }

        foreach (var warn in targets.SelectMany(x => x.Logs.Warn))
            MarkupLine(warn);
        foreach (var warn in Log.State.warnings.Reverse())
            MarkupLine(warn.Markup());

        if (!Log.State.warnings.Any() && !Log.State.errors.Any())
            MarkupLine($"\n");

        if (new[] { Log.State.errors.Count, targets.Sum(x => x.Logs.Error.Count) }.Sum() > 0)
        {
            var rule3 = new Rule($"[red bold]COMPILATION FAILED[/]") {Style = Style.Parse("lime rapidblink")};
            Write(rule3);
            MarkupLine($"\n");
            return -1;
        }
        else
        {
            var rule3 = new Rule($"[green bold]COMPILATION SUCCESS[/]") {Style = Style.Parse("lime rapidblink")};
            Write(rule3);
            MarkupLine($"\n");
            return 0;
        }
    }

}
