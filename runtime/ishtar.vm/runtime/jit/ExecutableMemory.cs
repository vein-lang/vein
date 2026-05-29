namespace ishtar;

/// <summary>
/// Cross-platform allocator for executable memory pages.
/// Allocates RW memory, writes code, then flips to RX.
/// Uses NativeLibrary for dynamic platform API resolution (no DllImport cross-contamination).
/// </summary>
public static unsafe class ExecutableMemory
{
    private static readonly nint _allocFn;
    private static readonly nint _protectFn;
    private static readonly nint _freeFn;
    private static readonly nint _flushFn;
    private static readonly nint _currentProcessFn;

    static ExecutableMemory()
    {
        if (OperatingSystem.IsWindows())
        {
            var kernel32 = NativeLibrary.Load("kernel32.dll");
            _allocFn = NativeLibrary.GetExport(kernel32, "VirtualAlloc");
            _protectFn = NativeLibrary.GetExport(kernel32, "VirtualProtect");
            _freeFn = NativeLibrary.GetExport(kernel32, "VirtualFree");
            _flushFn = NativeLibrary.GetExport(kernel32, "FlushInstructionCache");
            _currentProcessFn = NativeLibrary.GetExport(kernel32, "GetCurrentProcess");
        }
        else
        {
            var libc = NativeLibrary.Load("libc");
            _allocFn = NativeLibrary.GetExport(libc, "mmap");
            _protectFn = NativeLibrary.GetExport(libc, "mprotect");
            _freeFn = NativeLibrary.GetExport(libc, "munmap");
        }
    }

    public static void* Alloc(byte[] code)
    {
        var size = (nuint)code.Length;
        void* mem;

        if (OperatingSystem.IsWindows())
            mem = Windows_Alloc(size);
        else
            mem = Unix_Alloc(size);

        if (mem == null)
            throw new OutOfMemoryException("Failed to allocate executable memory");

        fixed (byte* src = code)
            NativeMemory.Copy(src, mem, size);

        if (OperatingSystem.IsWindows())
            Windows_MakeExecutable(mem, size);
        else
            Unix_MakeExecutable(mem, size);

        return mem;
    }

    public static void Free(void* ptr, nuint size)
    {
        if (OperatingSystem.IsWindows())
            Windows_Free(ptr, size);
        else
            Unix_Free(ptr, size);
    }

    // ─── Windows ──────────────────────────────────────────────────────────

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_EXECUTE_READ = 0x20;

    private static void* Windows_Alloc(nuint size)
    {
        var fn = (delegate* unmanaged[Stdcall]<void*, nuint, uint, uint, void*>)_allocFn;
        return fn(null, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    }

    private static void Windows_MakeExecutable(void* mem, nuint size)
    {
        var fn = (delegate* unmanaged[Stdcall]<void*, nuint, uint, uint*, int>)_protectFn;
        uint oldProtect;
        if (fn(mem, size, PAGE_EXECUTE_READ, &oldProtect) == 0)
            throw new InvalidOperationException("VirtualProtect failed");

        var flush = (delegate* unmanaged[Stdcall]<nint, void*, nuint, int>)_flushFn;
        var getProc = (delegate* unmanaged[Stdcall]<nint>)_currentProcessFn;
        flush(getProc(), mem, size);
    }

    private static void Windows_Free(void* ptr, nuint _)
    {
        var fn = (delegate* unmanaged[Stdcall]<void*, nuint, uint, int>)_freeFn;
        fn(ptr, 0, MEM_RELEASE);
    }

    // ─── Unix (Linux / macOS) ─────────────────────────────────────────────

    private const int PROT_READ = 0x1;
    private const int PROT_WRITE = 0x2;
    private const int PROT_EXEC = 0x4;
    private const int MAP_PRIVATE = 0x02;
    private const int MAP_ANONYMOUS_LINUX = 0x20;
    private const int MAP_ANONYMOUS_MAC = 0x1000;
    private const int MAP_JIT = 0x0800; // macOS ARM64

    private static void* Unix_Alloc(nuint size)
    {
        var fn = (delegate* unmanaged[Cdecl]<void*, nuint, int, int, int, long, void*>)_allocFn;

        var flags = MAP_PRIVATE;
        var prot = PROT_READ | PROT_WRITE;

        if (OperatingSystem.IsMacOS())
        {
            flags |= MAP_ANONYMOUS_MAC | MAP_JIT;
            prot |= PROT_EXEC; // macOS MAP_JIT requires RWX upfront
        }
        else
        {
            flags |= MAP_ANONYMOUS_LINUX;
        }

        var result = fn(null, size, prot, flags, -1, 0);
        if (result == (void*)-1)
            return null;
        return result;
    }

    private static void Unix_MakeExecutable(void* mem, nuint size)
    {
        if (OperatingSystem.IsMacOS())
            return; // MAP_JIT pages already have exec permission

        var fn = (delegate* unmanaged[Cdecl]<void*, nuint, int, int>)_protectFn;
        if (fn(mem, size, PROT_READ | PROT_EXEC) != 0)
            throw new InvalidOperationException("mprotect failed");
    }

    private static void Unix_Free(void* ptr, nuint size)
    {
        var fn = (delegate* unmanaged[Cdecl]<void*, nuint, int>)_freeFn;
        fn(ptr, size);
    }
}
