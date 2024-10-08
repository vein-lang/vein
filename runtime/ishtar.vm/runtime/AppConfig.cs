namespace ishtar.runtime;

using io;
using io.ini;

public readonly unsafe struct AppConfig
{
    public readonly IniRoot* rootCfg;

    public AppConfig(IniRoot* cfg)
    {
        rootCfg = cfg;
        Jit =  new AppConfig_Jit(rootCfg);
    }
    
    public bool UseDebugAllocator => rootCfg->GetGroup("vm").GetFlag("has_debug_allocator");
    public bool DisabledFinalization => rootCfg->GetGroup("vm").GetFlag("has_disabled_finalization");
    public bool CallOpCodeSkipValidateArgs => rootCfg->GetGroup("vm").GetFlag("skip_validate_args");
    public bool SkipValidateStfType => rootCfg->GetGroup("vm").GetFlag("skip_validate_stf_type");
    public bool DisableValidationInvocationArgs => rootCfg->GetGroup("vm").GetFlag("has_disabled_validation_inv_args");
    public bool UseConsole => rootCfg->GetGroup("vm").GetFlag("use_console");
    public bool NoTrace => rootCfg->GetGroup("vm").GetFlag("no_trace");
    public bool DeferThreadPool => rootCfg->GetGroup("vm:threading").GetFlag("defer");
    public long ThreadPoolSize => rootCfg->GetGroup("vm:threading").GetInt("size", -1);
    public bool PressEnterToExit => rootCfg->GetGroup("vm:debug").GetFlag("press_enter_to_exit");
    public SlicedString SnapshotPath => rootCfg->GetGroup("vm:debug").GetString("snapshot_path");
    public SlicedString EntryPoint => rootCfg->GetGroup("vm").GetString("entry_point");
    public SlicedString EntryPointClass => rootCfg->GetGroup("vm").GetString("entry_point_class");
    public SlicedString LibraryPath(string name) => rootCfg->GetGroup("vm:core").GetString(name);
    public bool UseNativeLoader => rootCfg->GetGroup("vm:core").GetFlag("use_loader");


    public readonly AppConfig_Jit Jit;

    public readonly struct AppConfig_Jit(IniRoot* rootCfg)
    {
        public bool Enabled => rootCfg->GetGroup("vm:jit").GetFlag("enable");
        public bool IsAutoTarget => rootCfg->GetGroup("vm:jit").GetString("target").SlicedStringEquals("auto");
        public bool EnableAsmParser => rootCfg->GetGroup("vm:jit").GetFlag("asm_parser");
        public bool EnableAsmPrinter => rootCfg->GetGroup("vm:jit").GetFlag("asm_printer");
        public bool EnableDisassembler => rootCfg->GetGroup("vm:jit").GetFlag("disassembler");
        public bool EnableTargetInfo => rootCfg->GetGroup("vm:jit").GetFlag("target_info");
        public bool EnableTargetMC => rootCfg->GetGroup("vm:jit").GetFlag("target_mc");
        public bool DeferContext => rootCfg->GetGroup("vm:jit").GetFlag("defer_context");
        public bool WriteIR => rootCfg->GetGroup("vm:jit").GetFlag("ir_write");
        public SlicedString IRPath => rootCfg->GetGroup("vm:jit").GetString("ir_path");
    }
}
