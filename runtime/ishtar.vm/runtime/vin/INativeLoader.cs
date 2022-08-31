namespace ishtar.runtime.vin;

public unsafe interface INativeLoader
{
    // TODO
    //static abstract delegate*<nint, string> ProviderFunction { get; }
    nint LoadLibrary(FileInfo fileName);
    nint LoadLibrary(string fileName);

    nint GetSymbol(nint handle, string symbol);
}
