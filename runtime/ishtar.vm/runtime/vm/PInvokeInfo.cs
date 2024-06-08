namespace ishtar
{
    using LLVMSharp.Interop;

    public unsafe struct PInvokeInfo : IEqualityComparer<PInvokeInfo>
    {
        public static PInvokeInfo Zero = default;

        public nint module_handle;
        public nint symbol_handle;
        public LLVMValueRef extern_function_declaration;
        public LLVMValueRef jitted_wrapper;
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
    }
}
