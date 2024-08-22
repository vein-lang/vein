namespace vein.cmd;

using resources;

[ExcludeFromCodeCoverage]
public class NewCommandSettings : CommandSettings
{
    [Description("Command execution is not affect files.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; set; }
}

[ExcludeFromCodeCoverage]
public class NewCommand(ShardRegistryQuery query, ShardProxy shardProxy) : AsyncCommand<NewCommandSettings>
{
    public static ValidationResult ValidateFileName(string fileName)
    {
        if (fileName.Contains(' '))
            return ValidationResult.Error("Space symbol is not allowed");

        var fi = default(FileInfo);
        try
        {
            fi = new FileInfo(fileName);
        }
        catch (ArgumentException e)
        {
            return ValidationResult.Error(e.Message);
        }
        catch (PathTooLongException e)
        {
            return ValidationResult.Error(e.Message);
        }
        catch (NotSupportedException e)
        {
            return ValidationResult.Error(e.Message);
        }

        if (!ReferenceEquals(fi, null))
            return ValidationResult.Success();

        return ValidationResult.Error("unknown invalid");
    }


    public override async Task<int> ExecuteAsync(CommandContext context, NewCommandSettings settings)
    {
        var curDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        var srcDir = curDir.SubDirectory("src").Ensure();
        var licenses = await Resources.Licenses.ReadAllLinesAsync();

        var name = context.Ask("Project name?", curDir.Name, ValidateFileName);
        var version = context.Ask("Project version?", "1.0.0.0", x => NuGetVersion.TryParse(x, out _));


        var author = context.Ask<string>("Enter your name:", x => !string.IsNullOrEmpty(x));
        var github = AnsiConsole.Ask<string>("Enter your github username:");
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
            project.Save(srcDir.File($"{name}.vproj"));
            await srcDir.File("app.vein").WriteAllTextAsync(
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
            var p = VeinProject.LoadFrom(srcDir.File($"{name}.vproj"));

            var installStdResult = await shardProxy.Install(new RunePackageKey("std"), p);

            if (installStdResult != 0)
            {
                Log.Warn($"[green]Success[/] created [orange]{name}[/] project, but install std library [red]failed[/].");
                return 0;
            }
        }
        Log.Info($"[green]Success[/] created [orange]{name}[/] project.");

        return 0;
    }

}
