namespace ishtar.runtime;

using collections;

public readonly unsafe struct RuntimeConstStorage
{
    private readonly DirectNativeDictionary<nint, IshtarObject>* storage =
        DirectNativeDictionary<IntPtr, IshtarObject>.New();

    public RuntimeConstStorage()
    {
    }


    public void Stage(RuntimeFieldName* name, IshtarObject* o) => storage->Add((nint)name, o);

    public IshtarObject* Get(RuntimeFieldName* name)
    {
        if (storage->TryGetValue((nint)name, out var result))
            return result;
        throw new KeyNotFoundException();
    }

    public List<(nint field, nint obj)> RawGetWithFilter(RuntimeStorageFilter filter)
    {
        var list = new List<(nint, nint)>();


        storage->ForEach(x =>
        {
            var k = (RuntimeFieldName*)x.Key;
            if (filter(k))
                list.Add((x.Key, x.Value));
        });
        return list;
    }
}

public unsafe delegate bool RuntimeStorageFilter(RuntimeFieldName* fieldName);
