namespace ishtar.native.windows;

using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;

[SupportedOSPlatform("windows10.0")]
public unsafe static class Native
{
    public static bool FreeLibrary(void* ptr) => PInvoke.FreeLibrary(new HINSTANCE((IntPtr)ptr)).Value == 1;
    
    public static void* LoadLibrary(string name)
    {
        var lib = PInvoke.LoadLibrary(name);
        
        if (lib.IsInvalid)
            throw new IshtarNativeLoadException($"{nameof(LoadLibrary)}(name: {name}) -> IsInvalid");

        return (void*)lib.DangerousGetHandle();
    }

    public static void* VirtualAlloc(void* lpAddr, nuint dwSize, AllocationType vat, MemoryProtection ppf)
        => PInvoke.VirtualAlloc(lpAddr, dwSize, (VIRTUAL_ALLOCATION_TYPE)(uint)vat, (PAGE_PROTECTION_FLAGS)(uint)ppf);
}



[Flags]
public enum AllocationType : uint
{
    Commit = 0x1000,
    Reserve = 0x2000,
    Reset = 0x80000,
    LargePages = 0x20000000,
    Physical = 0x400000,
    TopDown = 0x100000,
    WriteWatch = 0x200000,
    Decommit = 0x4000,
    Release = 0x8000
}

[Flags]
public enum MemoryProtection : uint
{
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    Guard = 0x100,
    NoCache = 0x200,
    WriteCombine = 0x400
}

[Flags]
public enum PageProtection
{
    NoAccess = 0x01,
    ReadOnly = 0x02,
    ReadWrite = 0x04,
    WriteCopy = 0x08,
    Execute = 0x10,
    ExecuteRead = 0x20,
    ExecuteReadWrite = 0x40,
    ExecuteWriteCopy = 0x80,
    Guard = 0x100,
    NoCache = 0x200,
    WriteCombine = 0x400,
}
