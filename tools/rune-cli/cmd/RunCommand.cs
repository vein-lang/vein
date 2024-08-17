namespace vein.cmd;

using System.ComponentModel;
using compiler.shared;
using project;
using Spectre.Console;
using Spectre.Console.Cli;


[ExcludeFromCodeCoverage]
public class RunSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Override generated boot config")]
    [CommandOption("--override-boot-cfg")]
    public string OverrideBootCfg { get; set; }

    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }
}

[ExcludeFromCodeCoverage]
public class RunCommand(WorkloadDb db) : AsyncCommandWithProject<RunSettings>
{
    // todo
    private static readonly string bootCfg =
        """
        [vm]
        skip_validate_args=true
        use_console=true
        no_trace=false

        [vm:jit]
        enable=true
        target="auto"
        asm_parser=false
        disassembler=false
        target_info=false
        target_mc=false
        asm_printer=false
        defer_context=true

        [vm:scheduler]
        defer_loop=true
        
        [vm:debug]
        press_enter_to_exit=false
        """;

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, VeinProject project)
    {
        var execFile = project.WorkDir.SubDirectory("bin").File($"{project.Name}.wll");

        if (!execFile.Exists)
        {
            var ask = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Project is [red]not[/] compiled, should start building?")
                    .AddChoices([
                        "Yes", "No", "I don't know"
                    ]));

            if (ask == "Yes")
            {
                await new BuildCommand(db).ExecuteAsync(new CommandContext(null, "", null, ["build"]), new CompileSettings
                {
                    Project = settings.Project
                }, project);
            }
            else if (ask == "No")
                return 0;
            else
            {
                Log.Info("It's sad to be you...");
                return -1;
            }
        }


        var tool = await db.TakeTool(PackageKey.VeinRuntimePackage, "ishtar", false);
        if (tool is null)
        {
            Log.Error($"'[orange]{PackageKey.VeinRuntimePackage}[/]' package not found, it may need to be installed by '[gray]rune workload install vein.runtime[/]'?");
            return -1;
        }

        var boot_config_data = bootCfg;

        if (!string.IsNullOrEmpty(settings.OverrideBootCfg))
        {
            var fullyPath = "";
            if (!Path.IsPathFullyQualified(settings.OverrideBootCfg))
            {
                if (settings.OverrideBootCfg.StartsWith("@")) // this a root of project
                    fullyPath = Path.Combine(project.WorkDir.FullName, settings.OverrideBootCfg.Replace("@", ""));
                else
                {
                    Log.Error($"Relative '[orange]{settings.OverrideBootCfg}[/]' not supported, use a @ symbol for mark a root path (at project base)");
                    return -1;
                }
            }


            var file = new FileInfo(fullyPath);

            if (!file.Exists)
            {
                Log.Error($"'[orange]{settings.OverrideBootCfg}[/]' not found, [red]failed[/] override boot config '[gray]rune workload install vein.compiler[/]'?");
                return -1;
            }

            boot_config_data = await file.ReadToEndAsync();
        }
        await project.WorkDir
            .SubDirectory("obj")
            .Ensure()
            .File("boot.ini")
            .WriteAllTextAsync(boot_config_data);
        

        return await new VeinIshtarProxy(tool, [execFile.FullName], project.WorkDir).ExecuteAsync();
    }
}
