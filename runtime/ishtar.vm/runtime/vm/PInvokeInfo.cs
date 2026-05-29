namespace ishtar
{
    [CTypeExport("ishtar_method_call_info_t")]
    public unsafe struct PInvokeInfo : IEqualityComparer<PInvokeInfo>, IEquatable<PInvokeInfo>
    {
        public static PInvokeInfo Zero = default;

        /// <summary>
        /// Handle to the loaded native library (from NativeLibrary.Load).
        /// </summary>
        public nint module_handle;

        /// <summary>
        /// Address of the exported native symbol (from NativeLibrary.GetExport).
        /// </summary>
        public nint symbol_handle;

        /// <summary>
        /// For internal methods: the compiled C# function pointer.
        /// For external methods: same as symbol_handle (the native entry point).
        /// </summary>
        [CTypeOverride("void*")]
        public nint compiled_func_ref;

        /// <summary>
        /// True for built-in C# method implementations (internal FFI).
        /// False for external native library imports (DllImport-style).
        /// </summary>
        public bool isInternal;

        public bool Equals(PInvokeInfo x, PInvokeInfo y)
            => x.module_handle == y.module_handle &&
               x.symbol_handle == y.symbol_handle &&
               x.compiled_func_ref == y.compiled_func_ref &&
               x.isInternal == y.isInternal;

        public int GetHashCode(PInvokeInfo obj)
            => HashCode.Combine(obj.module_handle, obj.symbol_handle, obj.compiled_func_ref, obj.isInternal);

        public bool Equals(PInvokeInfo other)
            => module_handle == other.module_handle &&
               symbol_handle == other.symbol_handle &&
               compiled_func_ref == other.compiled_func_ref &&
               isInternal == other.isInternal;

        public override bool Equals(object obj) => obj is PInvokeInfo other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(module_handle, symbol_handle, compiled_func_ref, isInternal);
    }
}
