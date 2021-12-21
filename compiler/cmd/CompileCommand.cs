namespace vein.cmd
{
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
    }

    public interface IProjectSettingProvider
    {
        string Project { get; set; }
    }

    public abstract class AsyncCommandWithProject<T> : CommandWithProject<T> where T : CommandSettings, IProjectSettingProvider
    {
        public sealed override int Execute(CommandContext ctx, T settigs, VeinProject project)
            => ExecuteAsync(ctx, settigs, project).Result;

        public abstract Task<int> ExecuteAsync(CommandContext ctx, T settigs, VeinProject project);
    }
    public abstract class CommandWithProject<T> : Command<T> where T : CommandSettings, IProjectSettingProvider
    {
        public sealed override int Execute(CommandContext ctx, T settings)
        {
            if (settings.Project is null)
            {
                var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());

                var projects = curDir.EnumerateFiles("*.vproj", SearchOption.AllDirectories)
                    .Where(x => !x.DirectoryName.Contains("bin"))
                    .Where(x => !x.DirectoryName.Contains("obj"))
                    .ToArray();

                if (!projects.Any())
                {
                    Log.Error($"Project not found in [orange]'{curDir.FullName}'[/] directory.");
                    return -1;
                }

                if (projects.Count() > 1)
                {
                    Log.Warn($"Multiple project detected.\n");

                    foreach (var (item, index) in projects.Select((x, y) => (x, y)))
                        MarkupLine($"({index}) [orange]'{item.Name}'[/] in [orange]'{item.Directory.FullName}'[/] directory.");
                    WriteLine();
                    var answer = Prompt(
                        new SelectionPrompt<string>()
                            .Title("Which project should build first?")
                            .PageSize(10)
                            .MoreChoicesText("[grey](Move up and down to reveal more projects)[/]")
                            .AddChoices(projects.Select((x, y) => x.FullName.Replace(curDir.FullName, ""))));

                    settings.Project = projects
                        .FirstOrDefault(x => x.FullName.Equals(Path.Combine(curDir.FullName, answer)))
                        .FullName;
                }
                else
                    settings.Project = projects.Single().FullName;
            }

            var name = Path.GetFileName(settings.Project);
            if (!File.Exists(settings.Project))
            {
                Log.Error($"Project [orange]'{name}'[/] not found.");
                return -1;
            }
            var project = VeinProject.LoadFrom(new(Path.GetFullPath(settings.Project)));

            if (!project.Sources.Any())
            {
                Log.Error($"Project [orange]'{name}'[/] has empty.");
                return -1;
            }

            return Execute(ctx, settings, project);
        }

        public abstract int Execute(CommandContext ctx, T settigs, VeinProject project);
    }


    [ExcludeFromCodeCoverage]
    public class CompileCommand : CommandWithProject<CompileSettings>
    {
        public override int Execute(CommandContext context, CompileSettings settings, VeinProject project)
        {
            Log.Info($"Project [orange]'{project.Name}'[/].");

            var targets = CompilationTask.Run(project.WorkDir, settings);


            foreach (var info in targets.SelectMany(x => x.Logs.Info).Reverse())
                MarkupLine(info.TrimEnd('\n'));
            foreach (var info in Log.infos)
                MarkupLine(info.TrimEnd('\n'));

            if (new[] { Log.errors.Count, targets.Sum(x => x.Logs.Error.Count) }.Sum() > 0)
            {
                var rule1 = new Rule($"[yellow]{Log.errors.Count} error found[/]") {Style = Style.Parse("red rapidblink")};
                Write(rule1);
            }

            foreach (var target in targets.SelectMany(x => x.Logs.Error).Reverse())
                MarkupLine(target);

            foreach (var error in Log.errors)
                MarkupLine(error);

            if (new[] { Log.warnings.Count, targets.Sum(x => x.Logs.Warn.Count) }.Sum() > 0)
            {
                var rule2 = new Rule($"[yellow]{Log.warnings.Count} warning found[/]") {Style = Style.Parse("orange rapidblink")};
                Write(rule2);
            }

            foreach (var warn in targets.SelectMany(x => x.Logs.Warn).Reverse())
                MarkupLine(warn);
            foreach (var warn in Log.warnings)
                MarkupLine(warn);

            if (!Log.warnings.Any() && !Log.errors.Any())
                MarkupLine($"\n\n\n");

            if (new[] { Log.errors.Count, targets.Sum(x => x.Logs.Error.Count) }.Sum() > 0)
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
}
