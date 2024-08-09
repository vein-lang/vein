namespace ishtar.collections;

public unsafe struct NativeQueue<T> : IDisposable where T : unmanaged, IEq<T>
{
    private T** items;
    private int capacity;
    private readonly AllocatorBlock _allocator;
    private int head;
    private int tail;
    private int count;

    public NativeQueue(int initialCapacity, AllocatorBlock allocator)
    {
        capacity = IshtarMath.max(initialCapacity, 16);
        _allocator = allocator;
        head = 0;
        tail = 0;
        count = 0;
        items = (T**)allocator.alloc((uint)(sizeof(T*) * capacity));
    }

    public static NativeQueue<T>* Create(int initial, AllocatorBlock allocator)
    {
        var r = (NativeQueue<T>*)allocator.alloc((uint)sizeof(NativeQueue<T>));
        *r = new NativeQueue<T>(initial, allocator);
        return r;
    }

    public static void Free(NativeQueue<T>* queue)
    {
        var allocator = queue->_allocator;
        queue->Dispose();
        allocator.free(queue);
    }

    public void Enqueue(T* value)
    {
        if (count == capacity)
            EnsureCapacity(capacity * 2);
        items[tail] = value;
        tail = (tail + 1) % capacity;
        count++;
    }

    public T* Dequeue()
    {
        if (count == 0)
            throw new InvalidOperationException("Queue is empty.");
        T* value = items[head];
        head = (head + 1) % capacity;
        count--;
        return value;
    }

    public bool TryDequeue(out T* value)
    {
        if (count == 0)
        {
            value = null;
            return false;
        }
        value = items[head];
        head = (head + 1) % capacity;
        count--;
        return true;
    }

    private void EnsureCapacity(int minCapacity)
    {
        int newCapacity = capacity == 0 ? 4 : capacity * 2;
        if (newCapacity < minCapacity)
        {
            newCapacity = minCapacity;
        }
        T** newItems = (T**)_allocator.realloc(items, (uint)(newCapacity * sizeof(T*)));
        if (newItems == null)
            throw new OutOfMemoryException("Failed to reallocate memory for queue.");
        if (head < tail)
            Unsafe.CopyBlock(newItems, items + head, (uint)(count * sizeof(T*)));
        else
        {
            Unsafe.CopyBlock(newItems, items + head, (uint)((capacity - head) * sizeof(T*)));
            Unsafe.CopyBlock(newItems + (capacity - head), items, (uint)(tail * sizeof(T*)));
        }
        head = 0;
        tail = count;
        items = newItems;
        capacity = newCapacity;
    }

    public void Clear()
    {
        capacity = 16;
        head = 0;
        tail = 0;
        count = 0;
        _allocator.free(items);
        items = (T**)_allocator.alloc((uint)(sizeof(T*) * capacity));
    }

    public int Count => count;

    public void Dispose() => _allocator.free(items);
}
