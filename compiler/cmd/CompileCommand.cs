namespace vein.cmd
{
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using compilation;
    using project;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using static Spectre.Console.AnsiConsole;

    public class CompileSettings : CommandSettings
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
    }
    public class CompileCommand : Command<CompileSettings>
    {
        public override int Execute(CommandContext context, CompileSettings settings)
        {
            if (settings.Project is null)
            {
                var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());

                var projects = curDir.EnumerateFiles("*.vproj", SearchOption.AllDirectories).ToArray();

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
                    var promt = new TextPrompt<int>("Which project should build first?")
                        .InvalidChoiceMessage("[red]That's not a valid input[/]")
                        .DefaultValue(0)
                        .AddChoices(projects.Select((x, y) => y));
                    
                    var answer = Prompt(promt);

                    settings.Project = projects[answer].FullName;
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

            if (project.SDK is null)
            {
                Log.Error($"SDK is not installed.");
                return -1;
            }

            project.Runtime ??= project.SDK.GetDefaultPack().Alias;


            Log.Info($"Project [orange]'{name}'[/].");
            Log.Info($"SDK [orange]'{project.SDK.Name} v{project.SDK.Version}'[/].");
            Log.Info($"Runtime [orange]'{project.Runtime}'[/].\n");


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
