namespace ishtar.collections;

public unsafe struct AtomicNativeDictionary<TKey, TValue> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
{
    private readonly AllocatorBlock _allocator;

    public struct Entry
    {
        public TKey Key;
        public TValue Value;
    }

    private Entry* entries;

    private int count;
    private int capacity;

    public static AtomicNativeDictionary<TKey, TValue>* Create(int initialCapacity, AllocatorBlock allocator)
    {
        var p = (AtomicNativeDictionary<TKey, TValue>*)allocator.alloc((uint)(sizeof(AtomicNativeDictionary<TKey, TValue>)));

        *p = new AtomicNativeDictionary<TKey, TValue>(initialCapacity, allocator);

        return p;
    }

    public AtomicNativeDictionary(int initialCapacity, AllocatorBlock allocator)
    {
        _allocator = allocator;
        capacity = IshtarMath.max(initialCapacity, 16);
        entries = (Entry*)_allocator.alloc((uint)(capacity * sizeof(Entry)));
        count = 0;
    }

    public void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value))
            throw new ArgumentException("An element with the same key already exists in the dictionary.");
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (ContainsKey(key))
            return false;

        if (count == capacity)
            Resize();

        entries[count].Key = key;
        entries[count].Value = value;
        count++;
        return true;
    }

    private void Resize()
    {
        capacity *= 2;
        entries = (Entry*)_allocator.realloc(entries, (uint)(capacity * sizeof(Entry)));
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

        entries[index] = entries[count - 1];
        count--;
        return true;
    }

    public bool ContainsKey(TKey key) => FindIndex(key) != -1;

    public ref TValue Get(TKey key)
    {
        int index = FindIndex(key);
        if (index == -1)
            throw new ArgumentException("Key not found in the dictionary.");

        return ref entries[index].Value;
    }

    public bool TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);

    public bool TryGet(TKey key, ref TValue value)
    {
        int index = FindIndex(key);
        if (index == -1)
        {
            value = default;
            return false;
        }

        value = entries[index].Value;
        return true;
    }

    public AtomicNativeList<TValue>* GetValues()
    {
        var values = AtomicNativeList<TValue>.Create(count, _allocator);
        for (int i = 0; i < count; i++)
            values->Add(entries[i].Value);
        return values;
    }

    private int FindIndex(TKey key)
    {
        if (count == 0) return -1;

        int hashCode = key.GetHashCode() & 0x7FFFFFFF;
        int index = hashCode % count;

        for (int i = index; i < count; i++)
        {
            if (entries[i].Key.Equals(key))
                return i;
        }

        for (int i = 0; i < index; i++)
        {
            if (entries[i].Key.Equals(key))
                return i;
        }

        return -1;
    }

    public void Clear()
    {
        _allocator.free(entries);
        capacity = 16;
        entries = (Entry*)_allocator.alloc((uint)(capacity * sizeof(Entry)));
        count = 0;
    }

    public int Count => count;


    public delegate void UnsafeForEach_Delegate(TKey key, ref TValue item);
    public void ForEach(UnsafeForEach_Delegate actor)
    {
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            actor(e.Key, ref e.Value);
        }
    }
    public delegate bool UnsafeFilter_Delegate(TKey key, ref TValue item);

    public bool Any(UnsafeFilter_Delegate filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            var e = entries[i];
            if (filter(e.Key, ref e.Value))
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
            var e = entries[i];
            all &= filter(e.Key, ref e.Value);
        }
        return all;
    }
}
