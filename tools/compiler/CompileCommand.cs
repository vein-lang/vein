namespace vein.cmd;

using compilation;
using project;
using Spectre.Console.Cli;
using static Spectre.Console.AnsiConsole;

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
