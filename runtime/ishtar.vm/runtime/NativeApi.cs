namespace ishtar;

using System.Runtime.InteropServices;

internal unsafe static class NativeApi
{
    #if LINUX
    // <asm/cachectl.h>
    //int cacheflush(char *addr, int nbytes, int cache);
    //mmap(PROT_READ | PROT_WRITE, MAP_PRIVATE)
    //mprotect
    #endif

    [DllImport("kernel32.dll")]
    internal static extern bool FlushInstructionCache(void* hProcess, void* lpBaseAddress, uint dwSize);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool VirtualProtect(void* lpAddress, uint dwSize, Protection flNewProtect, out Protection lpflOldProtect);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void* VirtualAlloc(void* lpAddress, uint dwSize, AllocationType lAllocationType, MemoryProtection flProtect);


    [Flags]
    public enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    public enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    public enum Protection
    {
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400
    }
}
