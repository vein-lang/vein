namespace ishtar.vin.loaders;

using ishtar.runtime.vin;
using System.Security;

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
[ExcludeFromCodeCoverage]
public class NullLoader : INativeLoader
{
    public IntPtr LoadLibrary(FileInfo fileName) => throw new NotImplementedException();

    public IntPtr LoadLibrary(string fileName) => throw new NotImplementedException();

    public IntPtr GetSymbol(IntPtr handle, string symbol) => throw new NotImplementedException();
}
