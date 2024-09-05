namespace vein.cmd;

using ishtar;
using project;

[ExcludeFromCodeCoverage]
public class RunSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }

    [Description("Enable printing profiler timings")]
    [CommandOption("--trace")]
    public bool TraceEnable { get; set; }
    [Description("Enable printing profiler timings")]
    [CommandOption("--show-profiler-timings")]
    public bool ProfilerDisplay { get; set; }
    [Description("Override generated boot config")]
    [CommandOption("--override-boot-cfg")]
    public string OverrideBootCfg { get; set; }

    [Description("Skip validate arguments parameters in vein methods when pass to execute")]
    [CommandOption("--sva")]
    public bool SkipValidateArgs { get; set; }

    [Description("Skip validate stage field opcode type")]
    [CommandOption("--svst")]
    public bool SkipValidateStfTypeOpCode { get; set; }

    [Description("User defer jit context")]
    [CommandOption("--jit-defer")]
    public bool JitContextDeffer { get; set; } = true;

    [Description("Override entry point method name (only static method, without arguments)")]
    [CommandOption("--entry-point", IsHidden = true)]
    public string EntryPoint { get; set; } = "master() -> [std]::std::Void";

    [Description("Override entry point class name")]
    [CommandOption("--entry-point-class", IsHidden = true)]
    public string EntryPointClass { get; set; } = "";

    [Description("Disable redirect stdout")]
    [CommandOption("--no-stdout")]
    public bool DoNotRedirectOutput { get; set; }
}

[ExcludeFromCodeCoverage]
public class RunCommand(WorkloadDb db) : AsyncCommandWithProject<RunSettings>
{
    // todo
    private static readonly string bootCfg =
        """
        [vm]
        skip_validate_args={SkipValidateArgs}
        use_console=true
        no_trace={TraceEnable}
        skip_validate_stf_type={SkipValidateStfTypeOpCode}
        entry_point="{EntryPoint}"
        entry_point_class="{EntryPointClass}"
        
        [vm:jit]
        enable=true
        target="auto"
        asm_parser=false
        disassembler=false
        target_info=false
        target_mc=false
        asm_printer=false
        defer_context={JitContextDeffer}
        ir_write=true
        ir_path="obj\"
        
        [vm:scheduler]
        defer_loop=false
        
        [vm:debug]
        press_enter_to_exit=false
        snapshot_path="obj\"
        
        [vm:threading]
        size=4
        defer=true
        
        [vm:core]
        use_loader={IsRelease}
        libgc="{GlobalPathLibGC}libgc"
        libuv="{GlobalPathLibUV}libuv"
        libLLVM="{GlobalPathLibLLVM}libLLVM"
        """;

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, VeinProject project)
    {
        using var tag = ScopeMetric.Begin("project.run")
            .WithProject(project);
        var execFile = project.WorkDir.SubDirectory("bin").File($"{project.Name}.wll");

        if (!execFile.Exists)
        {
            var ask = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Project is [red]not[/] compiled, should start building?")
                    .AddChoices([
                        "Yes", "No", "I don't know"
                    ]));

            switch (ask)
            {
                case "Yes":
                {
                    var result = await new BuildCommand(db).ExecuteAsync(new CommandContext(null, "", null, ["build", new FileInfo(settings.Project).FullName.Escapes('\"')]),
                        new CompileSettings { Project = new FileInfo(settings.Project).FullName }, project);

                    if (result != 0)
                    {
                        Log.Error($"Compilation failed");
                        return -1;
                    }

                    break;
                }
                case "No":
                    return 0;
                default:
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


        var libgc = await db.TakeSdkTarget("ishtar.libgc");
        var libuv = await db.TakeSdkTarget("ishtar.libuv");
        var llvm = await db.TakeSdkTarget("ishtar.llvm");



        var boot_config_data = bootCfg
            .Replace($"{{{nameof(settings.TraceEnable)}}}", (!settings.TraceEnable).ToString().ToLowerInvariant())
            .Replace($"{{{nameof(settings.SkipValidateArgs)}}}", settings.SkipValidateArgs.ToString().ToLowerInvariant())
            .Replace($"{{{nameof(settings.SkipValidateStfTypeOpCode)}}}", settings.SkipValidateStfTypeOpCode.ToString().ToLowerInvariant())
            .Replace($"{{{nameof(settings.EntryPoint)}}}", settings.EntryPoint)
            .Replace($"{{{nameof(settings.EntryPointClass)}}}", settings.EntryPointClass)
            .Replace($"{{{nameof(settings.JitContextDeffer)}}}", settings.JitContextDeffer.ToString().ToLowerInvariant())
#if DEBUG
            .Replace($"{{IsRelease}}", "false")
#else
            .Replace($"{{IsRelease}}", "true")
#endif
            .Replace($"{{GlobalPathLibGC}}", libgc!.FullName)
            .Replace($"{{GlobalPathLibUV}}", libuv!.FullName)
            .Replace($"{{GlobalPathLibLLVM}}", llvm!.FullName)

            ; 


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
        try
        {
            await project.WorkDir
                .SubDirectory("obj")
                .Ensure()
                .File("boot.ini")
                .WriteAllTextAsync(boot_config_data);

        }
        catch { }
        var envs = new Dictionary<string, string>();


        if (settings.ProfilerDisplay)
            envs.Add("VM_PROFILER", "true");
        

        var ishtarExitCode = await new VeinIshtarProxy(tool, [execFile.FullName.Escapes('\"')], project.WorkDir, envs, !settings.DoNotRedirectOutput).ExecuteAsync();

        if (!settings.DoNotRedirectOutput)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(ishtarExitCode != 0
                ? new Rule($"[red bold]RUN FAILED[/]") { Style = Style.Parse("deeppink3 rapidblink") }
                : new Rule($"[green bold]RUN SUCCESS[/]") { Style = Style.Parse("lime rapidblink") });
        }

        return ishtarExitCode;
    }
}
