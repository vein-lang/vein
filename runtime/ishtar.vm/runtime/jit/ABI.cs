namespace ishtar;

public unsafe class ABI(VirtualMachine vm)
{
    // ==================
    // TODO, move to outside
    // ==================

    private readonly Dictionary<string, NativeImportCache> _cache = new ();

    public void LoadNativeLibrary(NativeImportEntity entity, CallFrame* frame)
    {
        if (_cache.ContainsKey(entity.Entry))
            return;

        var result = frame->vm->NativeStorage.TryLoad(new FileInfo(entity.Entry), out var handle);

        if (!result)
        {
            frame->vm->FastFail(WNE.NATIVE_LIBRARY_COULD_NOT_LOAD, $"{entity.Entry}", frame);
            return;
        }

        _cache[entity.Entry] = new NativeImportCache(entity.Entry, handle);
    }

    public void LoadNativeSymbol(NativeImportEntity entity, CallFrame* frame)
    {
        var cached = _cache[entity.Entry];

        if (cached.ImportedSymbols.ContainsKey(entity.Fn))
            return;

        try
        {
            var symbol = frame->vm->NativeStorage.GetSymbol(cached.handle, entity.Fn);

            cached.ImportedSymbols.Add(entity.Fn, symbol);
            entity.Handle = symbol;
        }
        catch
        {
            frame->vm->FastFail(WNE.NATIVE_LIBRARY_SYMBOL_COULD_NOT_FOUND, $"{entity.Entry}::{entity.Fn}", frame);
        }
    }


}
