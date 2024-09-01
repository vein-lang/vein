namespace vein.cmd;

using resources;

[ExcludeFromCodeCoverage]
public class NewCommandSettings : CommandSettings
{
    [Description("Command execution is not affect files.")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; set; }

    [Description("Project name")]
    [CommandArgument(0, "[NAME]")]
    public string ProjectName { get; set; }

    [Description("Do not create subdirectory 'projectName'.")]
    [CommandOption("--dcs")]
    public bool DoNotCreateSubDirectory { get; set; }

    [Description("Force use interactive creation project")]
    [CommandOption("--interactive|-i")]
    public bool Interactive { get; set; }

    [Description("Set framework target name")]
    [CommandOption("--framework|-f", IsHidden = true)]
    public string FrameworkName { get; set; }

    [Description("Set template")]
    [CommandOption("--template|-t", IsHidden = true)]
    public string TemplateName { get; set; }
}

[ExcludeFromCodeCoverage]
public class NewCommand(ShardRegistryQuery query, ShardProxy shardProxy) : AsyncCommand<NewCommandSettings>
{
    private static ValidationResult ValidateFileName(string fileName)
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
        var licenses = await Resources.Licenses.ReadAllLinesAsync();
        var project = default(YAML.Project);
        var srcDir = default(DirectoryInfo);
        var name = "";

        if (string.IsNullOrEmpty(settings.ProjectName))
            settings.Interactive = true;

        if (settings.Interactive)
        {
            name = context.Ask<string>("Project name?", "", ValidateFileName);
            var version = context.Ask("Project version?", "0.0.0", x => NuGetVersion.TryParse(x, out _));

            srcDir = settings.DoNotCreateSubDirectory ?
                curDir.Ensure() :
                curDir.SubDirectory(name).Ensure();

            var author = context.Ask<string>("Enter your name:", x => !string.IsNullOrEmpty(x));
            var github = AnsiConsole.Ask<string>("Enter your github username:");
            var license = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose [green]license[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more licenses)[/]")
                    .AddChoices(licenses));

            project = new YAML.Project
            {
                Version = version,
                Authors = [new(author, github)],
                License = license
            };
        }
        else
        {
            name = settings.ProjectName;
            var version = "0.0.0";

            srcDir = settings.DoNotCreateSubDirectory ?
                curDir.Ensure() :
                curDir.SubDirectory(name).Ensure();

            var author = context.Ask<string>("Enter your name:", x => !string.IsNullOrEmpty(x));
            var github = AnsiConsole.Ask<string>("Enter your github username:");
            var license = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose [green]license[/]")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more licenses)[/]")
                    .AddChoices(licenses));

            project = new YAML.Project
            {
                Version = version,
                Authors = [new(author, github)],
                License = license
            };
        }

        

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
