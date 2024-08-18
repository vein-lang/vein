namespace vein.cmd;

using resources;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using NuGet.Versioning;
using project;

[ExcludeFromCodeCoverage]
public class NewCommandSettings : CommandSettings
{
    [Description("Command execution is not affect files.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; set; }
}

[ExcludeFromCodeCoverage]
public class NewCommand : Command<NewCommandSettings>
{
    public override int Execute(CommandContext context, NewCommandSettings settings)
    {
        var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var licenses = Resources.Licenses.ReadAllLines();

        var name = AnsiConsole.Ask("Project name?", curDir.Name);
        ask_version:
        var version = AnsiConsole.Ask("Project version?", "1.0.0.0");

        if (!NuGetVersion.TryParse(version, out _))
        {
            AnsiConsole.Markup($"Version [orange]'{version.EscapeMarkup()}'[/] is not valid semver version.");
            goto ask_version;
        }

        var author = AnsiConsole.Ask<string>("Enter your name:");
        var github = AnsiConsole.Ask<string>("Enter your github username:", "");
        var license = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose [green]license[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more licenses)[/]")
                .AddChoices(licenses));

        var project = new YAML.Project
        {
            Version = version,
            Authors = [new(author, github)],
            License = license
        };

        if (!settings.DryRun)
        {
            project.Save(curDir.File($"{name}.vproj"));
            curDir.File("app.vein").WriteAllText(
                $"""
                 #space "{name}"
                 #use "std"

                 class App {"{"}
                    public static master(): void
                    {"{"}
                        Out.print("hello world!");
                    {"}"}
                 {"}"}
                 """
            );
        }
        Log.Info($"[green]Success[/] created [orange]{name}[/] project.");

        return 0;
    }
}
