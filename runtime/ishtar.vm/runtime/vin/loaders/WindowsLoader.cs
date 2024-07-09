namespace ishtar.vin.loaders;

using System.Security;
using runtime.vin;

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
[ExcludeFromCodeCoverage]
public unsafe class WindowsLoader : INativeLoader
{
    public nint LoadLibrary(FileInfo fileName)
        => LoadLibrary(fileName.FullName);
    public nint LoadLibrary(string fileName)
        => LoadLibraryEx(fileName, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);

    public nint GetSymbol(nint handle, string symbol)
        => NativeLibrary.GetExport(handle, symbol);


    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string fileName, IntPtr reservedNull, uint flags);
}
