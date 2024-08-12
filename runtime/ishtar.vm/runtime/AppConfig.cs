namespace ishtar.runtime;

using io.ini;

public readonly unsafe struct AppConfig(IniRoot* rootCfg)
{
    public bool UseDebugAllocator => rootCfg->GetGroup("vm").GetFlag("has_debug_allocator");
    public bool DisabledFinalization => rootCfg->GetGroup("vm").GetFlag("has_disabled_finalization");
    public bool CallOpCodeSkipValidateArgs => rootCfg->GetGroup("vm").GetFlag("skip_validate_args");
    public bool SkipValidateStfType => rootCfg->GetGroup("vm").GetFlag("skip_validate_stf_type");
    public bool DisableValidationInvocationArgs => rootCfg->GetGroup("vm").GetFlag("has_disabled_validation_inv_args");
    public bool UseConsole => rootCfg->GetGroup("vm").GetFlag("use_console");
}
