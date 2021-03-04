namespace wave.cmd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using compilation;
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

            var directory = Path.GetDirectoryName(settings.Project);

            var files = Directory.EnumerateFiles(directory, "*.wave", SearchOption.AllDirectories).ToArray();

            if (!files.Any())
            {
                AnsiConsole.MarkupLine($"[red]ERR[/]: Project [orange]'{name}'[/] has empty.");
                return -1;
            }

            var c = Compiler.Process(files.Select(x => new FileInfo(x)).ToArray());
            
                
            foreach (var error in c.errors)
                AnsiConsole.MarkupLine($"[red]ERR[/]: {error}");
            foreach (var warn in c.warnings)
                AnsiConsole.MarkupLine($"[orange]WARN[/]: {warn}");

            return 0;
        }
    }
}