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
        [Description("Path to wproj file")]
        [CommandArgument(0, "[PROJECT]")]
        public string Project { get; set; }

        [Description("Display exported types table")]
        [CommandOption("--print-result-types")]
        public bool PrintResultType { get; set; }

        [Description("Compile into single file")]
        [CommandOption("--single-file|-s")]
        public bool HasSingleFile { get; set; }

        [Description("Wait to attach debbugger (ONLY DEBUG COMPILER)")]
        [CommandOption("--debugger|-d")]
        public bool IsNeedDebuggerAttach { get; set; }
    }
    public class CompileCommand : Command<CompileSettings>
    {
        public override int Execute(CommandContext context, CompileSettings settings)
        {
            if (settings.Project is null)
            {
                var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());

                var projects = curDir.EnumerateFiles("*.vproj").ToArray();

                if (!projects.Any())
                {
                    MarkupLine($"[red]ERR[/]: Project not found in [orange]'{curDir.FullName}'[/] directory.");
                    return -1;
                }

                if (projects.Count() > 1)
                {
                    MarkupLine($"[red]ERR[/]: Multiple project detected.");
                    foreach (var p in projects)
                        MarkupLine($"\t::[orange]'{p.Name}'[/] in [orange]'{curDir.FullName}'[/] directory.");
                    MarkupLine($"[red]ERR[/]: Specify project in [orange]'manac build [[PROJECT]]'[/]");
                    return -1;
                }

                settings.Project = projects.Single().FullName;
            }


            var name = Path.GetFileName(settings.Project);
            if (!File.Exists(settings.Project))
            {
                MarkupLine($"[red]ERR[/]: Project [orange]'{name}'[/] not found.");
                return -1;
            }
            var project = ManaProject.LoadFrom(new(Path.GetFullPath(settings.Project)));

            if (!project.Sources.Any())
            {
                MarkupLine($"[red]ERR[/]: Project [orange]'{name}'[/] has empty.");
                return -1;
            }

            if (project.SDK is null)
            {
                MarkupLine($"[red]ERR[/]: SDK is not installed.");
                return -1;
            }

            project.Runtime ??= project.SDK.GetDefaultPack().Alias;


            MarkupLine($"[blue]INF[/]: Project [orange]'{name}'[/].");
            MarkupLine($"[blue]INF[/]: SDK [orange]'{project.SDK.Name} v{project.SDK.Version}'[/].");
            MarkupLine($"[blue]INF[/]: Runtime [orange]'{project.Runtime}'[/].");


            var c = Compiler.Process(project.Sources.Select(x => new FileInfo(x)).ToArray(),
                project, settings);

            if (c.errors.Count > 0)
            {
                var rule1 = new Rule($"[yellow]{c.errors.Count} error found[/]") {Style = Style.Parse("red rapidblink")};
                Render(rule1);
            }

            foreach (var error in c.errors)
                MarkupLine($"[red]ERR[/]: {error}");

            if (c.warnings.Count > 0)
            {
                var rule2 = new Rule($"[yellow]{c.warnings.Count} warning found[/]") {Style = Style.Parse("orange rapidblink")};
                Render(rule2);
            }

            foreach (var warn in c.warnings)
                MarkupLine($"[orange]WARN[/]: {warn}");

            if (!c.warnings.Any() && !c.errors.Any())
                MarkupLine($"\n\n\n");

            if (c.errors.Count > 0)
            {

                var rule3 = new Rule($"[red bold]COMPILATION FAILED[/]") {Style = Style.Parse("lime rapidblink")};
                Render(rule3);
                MarkupLine($"\n");
                return -1;
            }
            else
            {
                var rule3 = new Rule($"[green bold]COMPILATION SUCCESS[/]") {Style = Style.Parse("lime rapidblink")};
                Render(rule3);
                MarkupLine($"\n");
                return 0;
            }
        }
    }
}
