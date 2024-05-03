namespace ishtar;

public class ABI(VirtualMachine vm)
{
    // ==================
    // TODO, move to outside
    // ==================

    private readonly Dictionary<string, NativeImportCache> _cache = new ();

    public void LoadNativeLibrary(NativeImportEntity entity, CallFrame frame)
    {
        if (_cache.ContainsKey(entity.entry))
            return;

        var result = frame.vm.NativeStorage.TryLoad(new FileInfo(entity.entry), out var handle);

        if (!result)
        {
            frame.vm.FastFail(WNE.NATIVE_LIBRARY_COULD_NOT_LOAD, $"{entity.entry}", frame);
            return;
        }

        _cache[entity.entry] = new NativeImportCache(entity.entry, handle);
    }

    public void LoadNativeSymbol(NativeImportEntity entity, CallFrame frame)
    {
        var cached = _cache[entity.entry];

        if (cached.ImportedSymbols.ContainsKey(entity.fn))
            return;

        try
        {
            var symbol = frame.vm.NativeStorage.GetSymbol(cached.handle, entity.fn);

            cached.ImportedSymbols.Add(entity.fn, symbol);
            entity.Handle = symbol;
        }
        catch
        {
            frame.vm.FastFail(WNE.NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND, $"{entity.entry}::{entity.fn}", frame);
        }
    }


}
