namespace ishtar.runtime.vin;

using ishtar.vin.loaders;

public static class NativeStorage
{
    private static readonly object guarder = new();
    private static Dictionary<string, nint> _cache = new();
    private static CallFrame selfFrame = IshtarFrames.NativeLoader();

    #if LINUX
    private static INativeLoader _loader = new LinuxLoader();
    #elif MACOS
    private static INativeLoader _loader = new MacOSLoader();
    #else
    private static INativeLoader _loader = new WindowsLoader();
    #endif

    public static nint GetSymbol(nint handle, string symbol)
        => _loader.GetSymbol(handle, symbol);

    public static bool TryLoad(FileInfo file, out nint result)
    {
        lock (guarder)
        {
            result = IntPtr.Zero;
            if (_cache.ContainsKey(file.Name))
            {
                result = _cache[file.Name];
                return true;
            }

            if (!file.Exists)
                return false;

            result = _cache[file.Name] = _loader.LoadLibrary(file);
            return true;
        }
    }

    public static nint LoadLibrary(FileInfo file)
    {
        lock (guarder)
        {
            if (_cache.ContainsKey(file.Name))
                return _cache[file.Name];
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
