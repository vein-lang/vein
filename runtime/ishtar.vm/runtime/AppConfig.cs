namespace ishtar.runtime;

using io;
using io.ini;

public readonly unsafe struct AppConfig
{
    public readonly IniRoot* rootCfg;

    public AppConfig(IniRoot* cfg)
    {
        rootCfg = cfg;
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
}
