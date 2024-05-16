namespace ishtar.runtime;

using collections;

public readonly unsafe struct RuntimeConstStorage
{
    private readonly NativeDictionary<nint, IshtarObject>* storage =
        IshtarGC.AllocateDictionary<IntPtr, IshtarObject>();

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


        storage->ForEach((key, value) =>
        {
            var k = (RuntimeFieldName*)key;
            if (filter(k))
                list.Add((key, (nint)value));
        });
        return list;
    }
}

public unsafe delegate bool RuntimeStorageFilter(RuntimeFieldName* fieldName);
