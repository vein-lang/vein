namespace ishtar.vm.runtime;

// TODO
public struct AppConfig
{
    public bool UseDebugAllocator => Environment.GetEnvironmentVariable("+vm:has_debug_allocator") is not null;
    public bool DisabledFinalization => Environment.GetEnvironmentVariable("+vm:has_disabled_finalization") is not null;
    public bool CallOpCodeSkipValidateArgs => Environment.GetEnvironmentVariable("+vm:skip-validate-args") is not null;
    public bool DisableValidationInvocationArgs => Environment.GetEnvironmentVariable("+vm:has_disabled_validation_inv_args") is not null;
}
