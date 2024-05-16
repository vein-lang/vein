namespace ishtar.collections;

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ishtar.runtime;

public unsafe delegate bool UnsafeComparer<T>(T* p1, T* p2) where T : unmanaged;

public static unsafe class NativeComparer<T> where T : unmanaged
{
    internal static UnsafeComparer<T> DefaultComparer;


    public static delegate*<T*, T*, bool> Delegate()
    {
        if (DefaultComparer is null)
            throw new NotImplementedException();
        //return (delegate*<T*, T*, bool>)Marshal.GetFunctionPointerForDelegate();
        return (delegate*<T*, T*, bool>)DefaultComparer.Method.MethodHandle.GetFunctionPointer();
    }

    static NativeComparer()
    {
        NativeComparer<RuntimeIshtarClass>.DefaultComparer = (p1, p2) => p1->Equals(p2);
        NativeComparer<RuntimeIshtarField>.DefaultComparer = (p1, p2) => p1->Name.Equals(p2->Name) && p1->Owner->Equals(p2->Owner);
        NativeComparer<RuntimeIshtarMethod>.DefaultComparer =
            (p1, p2) => p1->Name.Equals(p2->Name) && p1->Owner->Equals(p2->Owner);
        NativeComparer<RuntimeMethodArgument>.DefaultComparer =
            (p1, p2) => p1->Name->Equals(p2->Name) && p1->Type->Equals(p2->Type);
        NativeComparer<RuntimeIshtarModule>.DefaultComparer = (p1, p2) => p1->ID.Equals(p2->ID);
        NativeComparer<RuntimeQualityTypeName>.DefaultComparer = (p1, p2) => p1->Equals(p2);
        NativeComparer<RuntimeFieldName>.DefaultComparer = (p1, p2) => p1->Equals(p2);
        NativeComparer<RuntimeAspect>.DefaultComparer = (p1, p2) => p1->Name.Equals(p2->Name) && p1->Target == p2->Target;
        NativeComparer<RuntimeAspectArgument>.DefaultComparer = (p1, p2) => p1->Index.Equals(p2->Index) && p1->Owner->Name.Equals(p2->Owner->Name) && p1->Owner->Target == p2->Owner->Target;
        NativeComparer<InternedString>.DefaultComparer = (p1, p2) => p1->Equals(p2);

        // maybe not needed look at gc_id
        NativeComparer<IshtarObject>.DefaultComparer = (p1, p2) => p1->__gc_id.Equals(p2->__gc_id);


    }
}

public unsafe struct NativeList<T> : IDisposable where T : unmanaged
{
    private T** items;
    private int capacity;
    private readonly delegate* <T*, T*, bool> _comparer;
    private readonly AllocatorBlock _allocator;
    private int count;


    public NativeList(int initialCapacity, delegate*<T*, T*, bool> comparer, AllocatorBlock allocator)
    {
        capacity = IshtarMath.max(initialCapacity, 16);
        _comparer = comparer;
        _allocator = allocator;
        count = 0;
        items = (T**)allocator.alloc((uint)(sizeof(T*) * capacity));
    }

    public static NativeList<T>* Create(int initial, delegate*<T*, T*, bool> comparer, AllocatorBlock allocator)
    {
        var r = (NativeList<T>*)allocator.alloc((uint)sizeof(NativeList<T>));

        *r = new NativeList<T>(initial, comparer, allocator);

        return r;
    }

    public void Add(T* value)
    {
        if (count == capacity)
            EnsureCapacity(capacity * 2);
        items[count++] = value;
    }

    public void Remove(T* value)
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

        T** dst = items + index;
        T** src = dst + 1;
        count--;
        for (int i = index; i < count; i++)
            *dst++ = *src++;
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
            if (_comparer(items[i], value))
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
        for (int i = 0; i < count; i++)
            if (filter(items[i]))
                return items[i];
        return null;
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
