namespace vein;

using project;
using Spectre.Console;
using Spectre.Console.Cli;

public abstract class CommandWithProject<T> : Command<T> where T : CommandSettings, IProjectSettingProvider
{
    public sealed override int Execute(CommandContext ctx, T settings)
    {
        if (settings.Project is null)
        {
            var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            var projects = curDir.EnumerateFiles("*.vproj", SearchOption.AllDirectories)
                //.Where(x => !x.DirectoryName.Contains("bin"))
                //.Where(x => !x.DirectoryName.Contains("obj"))
                .ToArray();

            if (!projects.Any())
            {
                AnsiConsole.WriteLine($"Project not found in [orange]'{curDir.FullName}'[/] directory.");
                return -1;
            }

            if (projects.Count() > 1)
            {
                AnsiConsole.WriteLine($"Multiple project detected.\n");

                foreach (var (item, index) in projects.Select((x, y) => (x, y)))
                    AnsiConsole.MarkupLine($"({index}) [orange]'{item.Name}'[/] in [orange]'{item.Directory.FullName}'[/] directory.");
                AnsiConsole.WriteLine();
                var answer = AnsiConsole.Prompt(
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
            AnsiConsole.WriteLine($"Project [orange]'{name}'[/] not found.\nSearch folder: [orange]'{new FileInfo(settings.Project).FullName}'[/]");
            return -1;
        }
        var project = VeinProject.LoadFrom(new(Path.GetFullPath(settings.Project)));

        if (project.IsWorkload)
            return Execute(ctx, settings, project);
        if (!project.Sources.Any())
        {
            AnsiConsole.WriteLine($"Project [orange]'{name}'[/] has empty.");
            return -1;
        }

        return Execute(ctx, settings, project);
    }

    public abstract int Execute(CommandContext ctx, T settings, VeinProject project);
}
