namespace ishtar.collections;

public unsafe struct AtomicNativeList<T> where T : unmanaged, IEquatable<T>
{
    private T* items;
    private int capacity;
    private readonly AllocatorBlock _allocator;
    private int count;

    public AtomicNativeList(int initialCapacity, AllocatorBlock allocator)
    {
        capacity = IshtarMath.max(initialCapacity, 16);
        _allocator = allocator;
        count = 0;
        items = (T*)allocator.alloc_primitives((uint)(sizeof(T) * capacity));
    }

    public static AtomicNativeList<T>* Create(int initial, AllocatorBlock allocator)
    {
        var r = (AtomicNativeList<T>*)allocator.alloc((uint)sizeof(AtomicNativeList<T>));

        *r = new AtomicNativeList<T>(initial, allocator);

        return r;
    }

    public void Add(T value)
    {
        if (count == capacity)
            EnsureCapacity(capacity * 2);
        items[count++] = value;
    }

    public void Remove(T value)
    {
        int index = IndexOf(value);
        if (index != -1)
        {
            RemoveAt(index);
        }
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new IndexOutOfRangeException("Index is out of range.");
        }

        T* dst = items + index;
        T* src = dst + 1;
        count--;
        for (int i = index; i < count; i++)
            *dst++ = *src++;
    }

    public void AddRange(T* values, int sizeValues)
    {
        if (count + sizeValues >= capacity)
        {
            EnsureCapacity(count + sizeValues);
        }
        for (int i = 0; i < sizeValues; i++)
        {
            items[count++] = values[i];
        }
    }

    public void AddRange(AtomicNativeList<T>* lst)
    {
        if (count + lst->count >= capacity)
        {
            EnsureCapacity(count + lst->count);
        }
        for (int i = 0; i < lst->count; i++)
        {
            Add(lst->items[i]);
        }
    }

    private void EnsureCapacity(int minCapacity)
    {
        int newCapacity = capacity == 0 ? 4 : capacity * 2;
        if (newCapacity < minCapacity)
        {
            newCapacity = minCapacity;
        }
        T* newItems = (T*)_allocator.realloc(items, (uint)(newCapacity * sizeof(T)));
        if (newItems == null)
        {
            throw new OutOfMemoryException("Failed to reallocate memory for list.");
        }
        items = newItems;
        capacity = newCapacity;
    }

    private int IndexOf(T value)
    {
        for (int i = 0; i < count; i++)
        {
            if (items[i].Equals(value))
                return i;
        }
        return -1;
    }

    public T Get(int index)
    {
        if (count == 0) throw new CollectionIsEmpty();
        if (index > count) throw new ArgumentOutOfRangeException();
        return items[index];
    }

    public int Count => count;

    public void ForEach(SafeForEach_Delegate<T> actor)
    {
        for (int i = 0; i < count; i++) actor(ref items[i]);
    }

    public bool Any(SafeFilter_Delegate<T> filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            if (filter(ref items[i]))
                return true;
        }

        return false;
    }

    public bool All(SafeFilter_Delegate<T> filter)
    {
        if (count == 0) return false;
        var all = true;
        for (int i = 0; i < count; i++) all &= filter(ref items[i]);
        return all;
    }


    public bool Any(delegate*<ref T, bool> filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            if (filter(ref items[i]))
                return true;
        }

        return false;
    }

    public bool All(delegate*<ref T, bool> filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
            if (!filter(ref items[i]))
                return false;
        return true;
    }

    public void ForEach(delegate*<ref T, void> actor)
    {
        for (int i = 0; i < count; i++) actor(ref items[i]);
    }

    public ref T First()
    {
        if (count == 0) throw new CollectionIsEmpty();
        return ref items[0];
    }

    public ref T First(delegate*<ref T, bool> filter)
    {
        if (count == 0) throw new CollectionIsEmpty();
        for (int i = 0; i < count; i++)
            if (filter(ref items[i]))
                return ref items[i];
        throw new IterationWithoutResult();
    }
}


public delegate void SafeForEach_Delegate<T>(ref T item) where T : unmanaged, IEquatable<T>;
public delegate bool SafeFilter_Delegate<T>(ref T item) where T : unmanaged, IEquatable<T>;
