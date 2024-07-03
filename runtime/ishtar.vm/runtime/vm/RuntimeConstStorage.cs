namespace ishtar.runtime;

using collections;
using gc;

public readonly unsafe struct RuntimeConstStorage(RuntimeIshtarModule* module) : IDisposable
{
    private readonly RuntimeIshtarModule* _module = module;
    private readonly AtomicNativeDictionary<nint, stackval>* storage = IshtarGC.AllocateAtomicDictionary<IntPtr, stackval>(module);

    public void Dispose() => storage->Clear();


    public void Stage(RuntimeFieldName* name, stackval* o) => storage->Add((nint)name, *o);
    public void Stage(RuntimeFieldName* name, stackval o) => storage->Add((nint)name, o);

    public stackval Get(RuntimeFieldName* name)
    {
        if (storage->TryGetValue((nint)name, out var result))
            return result;
        throw new KeyNotFoundException();
    }

    public List<(nint field, stackval obj)> RawGetWithFilter(RuntimeStorageFilter filter)
    {
        var list = new List<(nint, stackval)>();


        storage->ForEach((IntPtr key, ref stackval item) =>
        {
            var k = (RuntimeFieldName*)key;
            if (filter(k))
                list.Add((key, item));
        });
        return list;
    }
}

public unsafe delegate bool RuntimeStorageFilter(RuntimeFieldName* fieldName);
