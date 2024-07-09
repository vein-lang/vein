namespace ishtar.collections;

using static libuv.LibUV;

public unsafe struct NativeConcurrentDictionary<TKey, TValue> where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
{
    private readonly NativeConcurrentDictionary<TKey, TValue>* _self;
    private readonly AllocatorBlock _allocator;


    private TKey* keys;
    private TValue** values;

    private int count;
    private int capacity;
    private uv_sem_t semaphore;

    public static NativeConcurrentDictionary<TKey, TValue>* Create(int initialCapacity, AllocatorBlock allocator)
    {
        var p = (NativeConcurrentDictionary<TKey, TValue>*)allocator.alloc((uint)(sizeof(NativeConcurrentDictionary<TKey, TValue>)));

        *p = new NativeConcurrentDictionary<TKey, TValue>(initialCapacity, p, allocator);

        var result = uv_sem_init(out p->semaphore, 1);

        VirtualMachine.Assert(result == 0, WNE.SEMAPHORE_FAILED, "failed initialize NativeConcurrentDictionary");

        return p;
    }

    private readonly struct sem_slim : IDisposable
    {
        private readonly NativeConcurrentDictionary<TKey, TValue>* _dict;

        public sem_slim(NativeConcurrentDictionary<TKey, TValue>* dict)
        {
            _dict = dict;
            uv_sem_wait(ref _dict->semaphore);
        }
        public void Dispose() => uv_sem_post(ref _dict->semaphore);
    }

    public static void Free(NativeConcurrentDictionary<TKey, TValue>* dict)
    {
        var allocator = dict->_allocator;
        allocator.free(dict->keys);
        allocator.free(dict->values);
        uv_sem_destroy(ref dict->semaphore);
        allocator.free(dict);
    }

    public NativeConcurrentDictionary(int initialCapacity, NativeConcurrentDictionary<TKey, TValue>* self, AllocatorBlock allocator)
    {
        _self = self;
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
        using var locker = new sem_slim(_self);
        if (FindIndex(key) != -1) // do not use ContainsKey
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
        using var locker = new sem_slim(_self);

        int index = FindIndex(key);
        if (index == -1)
            return false;

        keys[index] = keys[count - 1];
        values[index] = values[count - 1];
        count--;
        return true;
    }

    public bool ContainsKey(TKey key)
    {
        using var locker = new sem_slim(_self);
        return FindIndex(key) != -1;
    }

    public TValue* Get(TKey key)
    {
        using var locker = new sem_slim(_self);
        int index = FindIndex(key);
        if (index == -1)
            throw new ArgumentException("Key not found in the dictionary.");
        return values[index];
    }

    public bool TryGetValue(TKey key, out TValue* value)
    {
        using var locker = new sem_slim(_self);
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
        using var locker = new sem_slim(_self);
        _allocator.free(keys);
        _allocator.free(values);
        capacity = 16;
        keys = (TKey*)_allocator.alloc((uint)(capacity * sizeof(TKey)));
        values = (TValue**)_allocator.alloc((uint)(capacity * sizeof(TValue*)));
        count = 0;
    }

    public int Count => count;
}

