namespace ishtar.vin.loaders;

using runtime.vin;
using System.Runtime.InteropServices;
using System.Security;

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
[ExcludeFromCodeCoverage]
public class UnixLoader : INativeLoader
{
    public nint LoadLibrary(FileInfo fileName)
        => LoadLibrary(fileName.FullName);

    public nint LoadLibrary(string fileName)
        => dlopen(fileName, RTLD_NOW);

    private const int RTLD_NOW = 2;

    public nint GetSymbol(nint handle, string symbol)
        => NativeLibrary.GetExport(handle, symbol);

    [DllImport("libdl.so", SetLastError = true)]
    private static extern nint dlopen(string fileName, int flags);
}
