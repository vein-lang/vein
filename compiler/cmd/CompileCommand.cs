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
                    Log.Error($"Project not found in [orange]'{curDir.FullName}'[/] directory.");
                    return -1;
                }

                if (projects.Count() > 1)
                {
                    Log.Error($"Multiple project detected.");
                    foreach (var p in projects)
                        Log.Error($"\t::[orange]'{p.Name}'[/] in [orange]'{curDir.FullName}'[/] directory.");
                    Log.Error($"Specify project in [orange]'manac build [[PROJECT]]'[/]");
                    return -1;
                }

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


            CompilationTask.Run(project.WorkDir);


            foreach (var info in Log.infos)
                MarkupLine(info.TrimEnd('\n'));

            if (Log.errors.Count > 0)
            {
                var rule1 = new Rule($"[yellow]{Log.errors.Count} error found[/]") {Style = Style.Parse("red rapidblink")};
                Render(rule1);
            }

            foreach (var error in Log.errors)
                MarkupLine(error);

            if (Log.warnings.Count > 0)
            {
                var rule2 = new Rule($"[yellow]{Log.warnings.Count} warning found[/]") {Style = Style.Parse("orange rapidblink")};
                Render(rule2);
            }

            foreach (var warn in Log.warnings)
                MarkupLine(warn);

            if (!Log.warnings.Any() && !Log.errors.Any())
                MarkupLine($"\n\n\n");

            if (Log.errors.Count > 0)
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
