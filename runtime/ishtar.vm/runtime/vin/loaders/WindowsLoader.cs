namespace ishtar.vin.loaders;

using System.Runtime.InteropServices;
using runtime.vin;

public unsafe class WindowsLoader : INativeLoader
{
    public static nint LoadLibrary(FileInfo fileName)
        => LoadLibrary(fileName.FullName);
    public static nint LoadLibrary(string fileName)
        => LoadLibraryEx(fileName, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);


    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string fileName, IntPtr reservedNull, uint flags);
}
