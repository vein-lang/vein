namespace ishtar.runtime.vin;

using ishtar.vin.loaders;

public class NativeStorage(VirtualMachine vm)
{
    private readonly object guarder = new();
    private readonly Dictionary<string, nint> _cache = new();

#if LINUX
    private readonly INativeLoader _loader = new UnixLoader();
#elif MACOS
#error MacOS is not supported currently
    private readonly INativeLoader _loader = new NullLoader();
#else
    private readonly INativeLoader _loader = new WindowsLoader();
#endif

    public nint GetSymbol(nint handle, string symbol)
        => _loader.GetSymbol(handle, symbol);

    public bool TryLoad(FileInfo file, out nint result)
    {
        lock (guarder)
        {
            result = IntPtr.Zero;
            if (_cache.TryGetValue(file.Name, out result))
                return true;

            if (!file.Exists)
                return false;

            result = _cache[file.Name] = _loader.LoadLibrary(file);
            return true;
        }
    }

    public nint LoadLibrary(FileInfo file)
    {
        lock (guarder)
        {
            if (_cache.TryGetValue(file.Name, out IntPtr library))
                return library;
            return _cache[file.Name] = _loader.LoadLibrary(file);
        }
    }
}


public static class Arch
{
    public const string X64 = "x64";
    public const string ARM = "arm";
    public const string ARM64 = "arm64";
}
