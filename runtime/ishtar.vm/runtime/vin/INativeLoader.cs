namespace ishtar.runtime.vin;

public unsafe interface INativeLoader
{
    // TODO
    //static abstract delegate*<nint, string> ProviderFunction { get; }
    static abstract nint LoadLibrary(FileInfo fileName);
    static abstract nint LoadLibrary(string fileName);
}
