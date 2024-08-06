namespace ishtar.collections;

using System.Collections;
using System.Runtime.CompilerServices;
using ishtar.runtime;
using vein.collections;

public unsafe delegate bool UnsafeComparer<T>(T* p1, T* p2) where T : unmanaged;

public unsafe interface IEq<T> where T : unmanaged
{
    static abstract bool Eq(T* p1, T* p2);
}

public unsafe interface IDirectEq<T> where T : unmanaged
{
    static abstract bool Eq(ref T p1, ref T p2);
}

public unsafe struct NativeList<T> : IDisposable where T : unmanaged, IEq<T>
{
    private T** items;
    private int capacity;
    private readonly AllocatorBlock _allocator;
    private int count;


    public NativeList(int initialCapacity, AllocatorBlock allocator)
    {
        capacity = IshtarMath.max(initialCapacity, 16);
        _allocator = allocator;
        count = 0;
        items = (T**)allocator.alloc((uint)(sizeof(T*) * capacity));
    }

    public static NativeList<T>* Create(int initial, AllocatorBlock allocator)
    {
        var r = (NativeList<T>*)allocator.alloc((uint)sizeof(NativeList<T>));

        *r = new NativeList<T>(initial, allocator);

        return r;
    }

    public static void Free(NativeList<T>* list)
    {
        var allocator = list->_allocator;
        list->Dispose();
        allocator.free(list);
    }


    public void Add(T* value)
    {
        if (count == capacity)
            EnsureCapacity(capacity * 2);

#if DEBUG
        if (IndexOf(value) != -1)
            throw new DuplicateItemException("");
#endif

        items[count++] = value;
    }

    public void Remove(T* value)
    {
        int index = IndexOf(value);
        if (index != -1)
        {
            RemoveAt(index);
            return;
        }

        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
            throw new IndexOutOfRangeException("Index is out of range.");

        T** dst = items + index;
        T** src = dst + 1;
        count--;
        for (int i = index; i < count; i++) *dst++ = *src++;
    }

    public void AddRange(T** values, int sizeValues)
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

    public void AddRange(NativeList<T>* lst)
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
        T** newItems = (T**)_allocator.realloc(items, (uint)(newCapacity * sizeof(T*)));
        if (newItems == null)
        {
            throw new OutOfMemoryException("Failed to reallocate memory for list.");
        }
        items = newItems;
        capacity = newCapacity;
    }

    private int IndexOf(T* value)
    {
        for (int i = 0; i < count; i++)
        {
            if (T.Eq(items[i], value))
                return i;
        }
        return -1;
    }

    public void Clear()
    {
        capacity = 16;
        count = 0;
        _allocator.free(items);
        items = (T**)_allocator.alloc((uint)(sizeof(T*) * capacity));
    }

    public int Count => count;
    public int Length => count;

    public T* Get(int index)
    {
        if (count == 0) throw new CollectionIsEmpty();
        if (index > count) throw new ArgumentOutOfRangeException();
        return items[index];
    }


    public void ForEach(UnsafeForEach_Delegate<T> actor)
    {
        for (int i = 0; i < count; i++) actor(items[i]);
    }

    public List<T> ToList()
    {
        var lst = new List<T>();
        for (int i = 0; i < count; i++) lst.Add(*items[i]);
        return lst;
    }

    public bool Any(UnsafeFilter_Delegate<T> filter)
    {
        if (count == 0) return false;
        for (int i = 0; i < count; i++)
        {
            if (filter(items[i]))
                return true;
        }

        return false;
    }

    public bool All(UnsafeFilter_Delegate<T> filter)
    {
        if (count == 0) return false;
        var all = true;
        for (int i = 0; i < count; i++) all &= filter(items[i]);
        return all;
    }

    public T* First()
    {
        if (count == 0) throw new CollectionIsEmpty();
        return items[0];
    }

    public T* First(UnsafeFilter_Delegate<T> filter)
    {
        if (count == 0) throw new CollectionIsEmpty();
        for (int i = 0; i < count; i++)
            if (filter(items[i]))
                return items[i];
        throw new IterationWithoutResult();
    }

    public T* FirstOrNull(UnsafeFilter_Delegate<T> filter)
    {
        if (count == 0) return null;
#if !DEBUG

        for (int i = 0; i < count; i++)
            if (filter(items[i]))
                return items[i];
        return null;
#else
        var item = default(T*);
        for (int i = 0; i < count; i++)
            if (filter(items[i]))
            {
                if (item is not null)
                    throw new DuplicateItemException("");
                item = items[i];
            }
        return item;
#endif
    }

    public void Swap(T* from, T* to)
    {
        Remove(from);
        Add(to);
    }

    public void Dispose() => _allocator.free(items);


    public Enumerator GetEnumerator() => new() { m_Ptr = items, m_Length = Length, m_Index = -1 };

    public struct Enumerator : IEnumerator<nint>
    {
        internal T** m_Ptr;
        internal int m_Length;
        internal int m_Index;

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Advances the enumerator to the next element of the list.
        /// </summary>
        /// <remarks>
        /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
        /// </remarks>
        /// <returns>True if `Current` is valid to read after the call.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_Index < m_Length;

        /// <summary>
        /// Resets the enumerator to its initial state.
        /// </summary>
        public void Reset() => m_Index = -1;

        object IEnumerator.Current => Current;

        /// <summary>
        /// The current element.
        /// </summary>
        /// <value>The current element.</value>
        public nint Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (nint)m_Ptr[m_Index];
        }
    }
}

public unsafe delegate void UnsafeForEach_Delegate<T>(T* item) where T : unmanaged;
public unsafe delegate bool UnsafeFilter_Delegate<T>(T* item) where T : unmanaged;
public unsafe delegate void UnsafeVoid_Delegate<T>(T* item) where T : unmanaged;
