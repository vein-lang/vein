namespace ishtar.runtime;

using static ishtar.runtime.gc.BoehmGCLayout.Native;
using static libuv.LibUV;

public static unsafe class libuv_gc_allocator
{
    public static void install() => uv_replace_allocator(mallocFunc, reallocFunc, сallocFunc, FreeFunc);

    private static IntPtr mallocFunc(UIntPtr size)
    {
        if (!GC_thread_is_registered())
            throw new InvalidOperationException($"[malloc] trying allocation in unregistered thread, this a bug," +
                                                $" Please report the problem into https://github.com/vein-lang/vein/issues'");
        return (nint)GC_malloc((nuint)size);
    }

    private static IntPtr reallocFunc(IntPtr ptr, UIntPtr size)
        => (nint)GC_realloc(ptr, (nint)(nuint)size);

    private static IntPtr сallocFunc(UIntPtr count, UIntPtr size)
    {
        if (!GC_thread_is_registered())
            throw new InvalidOperationException($"[сalloc] trying allocation in unregistered thread, this a bug," +
                                                $" Please report the problem into https://github.com/vein-lang/vein/issues'");

        var totalSize = (nuint)count * (nuint)size;
        var ptr = (byte*)GC_malloc(totalSize);

        new Span<byte>(ptr, (int)totalSize).Clear();

        return (nint)ptr;
    }

    private static void FreeFunc(IntPtr ptr) => GC_free((void*)ptr);
}
