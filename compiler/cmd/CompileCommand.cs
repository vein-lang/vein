namespace insomnia.cmd
{
    using System;
    using System.IO;
    using System.Linq;
    using compilation;
    using project;
    using Spectre.Console;
    using Spectre.Console.Cli;
    public class CompileSettings : CommandSettings
    {
        [CommandArgument(0, "[PROJECT]")]
        public string Project { get; set; }
    }
    public class CompileCommand : Command<CompileSettings>
    {
        public override int Execute(CommandContext context, CompileSettings settings)
        {
            var name = Path.GetFileName(settings.Project);
            if (!File.Exists(settings.Project))
            {
                AnsiConsole.MarkupLine($"[red]ERR[/]: Project [orange]'{name}'[/] not found.");
                return -1;
            }
            var project = WaveProject.LoadFrom(new(Path.GetFullPath(settings.Project)));
            
            if (!project.Sources.Any())
            {
                AnsiConsole.MarkupLine($"[red]ERR[/]: Project [orange]'{name}'[/] has empty.");
                return -1;
            }

            
            

            

            var c = Compiler.Process(project.Sources.Select(x => new FileInfo(x)).ToArray(), project);

            if (c.errors.Count > 0)
            {
                var rule1 = new Rule($"[yellow]{c.errors.Count} error found[/]") {Style = Style.Parse("red rapidblink")};
                AnsiConsole.Render(rule1);
            }
            
            foreach (var error in c.errors)
                AnsiConsole.MarkupLine($"[red]ERR[/]: {error}");

            if (c.warnings.Count > 0)
            {
                var rule2 = new Rule($"[yellow]{c.warnings.Count} warning found[/]") {Style = Style.Parse("orange rapidblink")};
                AnsiConsole.Render(rule2);
            }

            foreach (var warn in c.warnings)
                AnsiConsole.MarkupLine($"[orange]WARN[/]: {warn}");
            
            AnsiConsole.MarkupLine($"\n\n\n");
            if (c.errors.Count > 0)
            {
                var rule3 = new Rule($"[red bold]COMPILATION FAILED[/]") {Style = Style.Parse("lime rapidblink")};
                AnsiConsole.Render(rule3);
                AnsiConsole.MarkupLine($"\n");
                return -1;
            }
            else
            {
                var rule3 = new Rule($"[green bold]COMPILATION SUCCESS[/]") {Style = Style.Parse("lime rapidblink")};
                AnsiConsole.Render(rule3);
                AnsiConsole.MarkupLine($"\n");
                return 0;
            }
        }
    }
}