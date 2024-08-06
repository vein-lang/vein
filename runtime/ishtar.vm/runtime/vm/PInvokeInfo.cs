namespace ishtar
{
    using LLVMSharp.Interop;

    [CTypeExport("ishtar_method_call_info_t")]
    public unsafe struct PInvokeInfo : IEqualityComparer<PInvokeInfo>, IEquatable<PInvokeInfo>
    {
        public static PInvokeInfo Zero = default;

        public nint module_handle;
        public nint symbol_handle;
        [CTypeOverride("void*")]
        public LLVMValueRef extern_function_declaration;
        [CTypeOverride("void*")]
        public LLVMValueRef jitted_wrapper;
        [CTypeOverride("void*")]
        public nint compiled_func_ref;
        public bool isInternal;
        
        public void create_bindings(LLVMExecutionEngineRef engine)
        {
            engine.AddGlobalMapping(extern_function_declaration, symbol_handle);
            compiled_func_ref = engine.GetPointerToGlobal(jitted_wrapper);
        }

        public bool Equals(PInvokeInfo x, PInvokeInfo y)
            => x.module_handle == y.module_handle &&
               x.symbol_handle == y.symbol_handle &&
               x.extern_function_declaration.Equals(y.extern_function_declaration) &&
               x.jitted_wrapper.Equals(y.jitted_wrapper) &&
               x.compiled_func_ref == y.compiled_func_ref &&
               x.isInternal == y.isInternal;

        public int GetHashCode(PInvokeInfo obj)
            => HashCode.Combine(obj.module_handle, obj.symbol_handle, obj.extern_function_declaration, obj.jitted_wrapper, obj.compiled_func_ref, obj.isInternal);

        public bool Equals(PInvokeInfo other) => module_handle == other.module_handle && symbol_handle == other.symbol_handle && extern_function_declaration.Equals(other.extern_function_declaration) && jitted_wrapper.Equals(other.jitted_wrapper) && compiled_func_ref == other.compiled_func_ref && isInternal == other.isInternal;

        public override bool Equals(object obj) => obj is PInvokeInfo other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(module_handle, symbol_handle, extern_function_declaration, jitted_wrapper, compiled_func_ref, isInternal);
    }
}
