namespace ishtar.collections;

public unsafe struct NativeDictionary<TKey, TValue> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
{
    private readonly AllocatorBlock _allocator;


    private TKey* keys;
    private TValue** values;

    private int count;
    private int capacity;

    public static NativeDictionary<TKey, TValue>* Create(int initialCapacity, AllocatorBlock allocator)
    {
        var p = (NativeDictionary<TKey, TValue>*)allocator.alloc((uint)(sizeof(NativeDictionary<TKey, TValue>)));

        *p = new NativeDictionary<TKey, TValue>(initialCapacity, allocator);

        return p;
    }

    public static void Free(NativeDictionary<TKey, TValue>* dict)
    {
        var allocator = dict->_allocator;
        allocator.free(dict->keys);
        allocator.free(dict->values);
        allocator.free(dict);
    }

    public NativeDictionary(int initialCapacity, AllocatorBlock allocator)
    {
        _allocator = allocator;
        capacity = IshtarMath.max(initialCapacity, 16);
        keys = (TKey*)_allocator.alloc_primitives((uint)(capacity * sizeof(TKey)));
        values = (TValue**)_allocator.alloc_primitives((uint)(capacity * sizeof(TValue*)));
        count = 0;
    }

    public void Add(TKey key, TValue* value)
    {
        if (!TryAdd(key, value))
            throw new ArgumentException("An element with the same key already exists in the dictionary.");
    }

    public bool TryAdd(TKey key, TValue* value)
    {
        if (ContainsKey(key))
            return false;

        if (count == capacity)
            Resize();

        keys[count] = key;
        values[count] = value;
        count++;
        return true;
    }

    private void Resize()
    {
        capacity *= 2;

        keys = (TKey*)_allocator.realloc(keys, (uint)(capacity * sizeof(TKey)));
        values = (TValue**)_allocator.realloc(values, (uint)(capacity * sizeof(TValue*)));
    }

    public void Remove(TKey key)
    {
        if (!TryRemove(key))
            throw new ArgumentException("Key not found in the dictionary.");
    }

    public bool TryRemove(TKey key)
    {
        int index = FindIndex(key);
        if (index == -1)
            return false;

        keys[index] = keys[count - 1];
        values[index] = values[count - 1];
        count--;
        return true;
    }

    public bool ContainsKey(TKey key) => FindIndex(key) != -1;

    public TValue* Get(TKey key)
    {
        int index = FindIndex(key);
        if (index == -1)
            throw new ArgumentException("Key not found in the dictionary.");

        return values[index];
    }

    public bool TryGetValue(TKey key, out TValue* value)
    {
        int index = FindIndex(key);
        if (index == -1)
        {
            value = null;
            return false;
        }

        value = values[index];
        return true;
    }

    private int FindIndex(TKey key)
    {
        if (count == 0) return -1;

        int hashCode = key.GetHashCode() & 0x7FFFFFFF;
        int index = hashCode % count;

        for (int i = index; i < count; i++)
        {
            if (keys[i].Equals(key))
                return i;
        }

        for (int i = 0; i < index; i++)
        {
            if (keys[i].Equals(key))
                return i;
        }

        return -1;
    }

    public void Clear()
    {
        _allocator.free(keys);
        _allocator.free(values);
        capacity = 16;
        keys = (TKey*)_allocator.alloc((uint)(capacity * sizeof(TKey)));
        values = (TValue**)_allocator.alloc((uint)(capacity * sizeof(TValue*)));
        count = 0;
    }

    public int Count => count;


    public delegate void UnsafeForEach_Delegate(TKey key, TValue* item);
    public void ForEach(UnsafeForEach_Delegate actor)
    {
        for (int i = 0; i < count; i++) actor(keys[i], values[i]);
    }
    public delegate bool UnsafeFilter_Delegate(TKey key, TValue* item);

    public bool Any(UnsafeFilter_Delegate filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            if (filter(keys[i], values[i]))
                return true;
        }

        return false;
    }

    public bool All(UnsafeFilter_Delegate filter)
    {
        if (count == 0) return false;
        var all = true;
        for (int i = 0; i < count; i++)
        {
            all &= filter(keys[i], values[i]);
        }
        return all;
    }
}

