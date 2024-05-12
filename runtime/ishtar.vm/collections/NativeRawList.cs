namespace ishtar.collections;

using System.Collections;
using System.Runtime.CompilerServices;
using runtime;
using vm;

public unsafe struct NativeRawList<T> : IDisposable, INativeList<T>, IEnumerable<T>
    where T : unmanaged
{
    public T* Ptr;
    public int m_length;
    public int m_capacity;
    public int Length
    {
        readonly get => m_length;

        set
        {
            if (value > Capacity)
                Resize(value);
            else
                m_length = value;
        }
    }

    public int Size
    {
        get => Length;
        set => Length = value;
    }

    public int Capacity
    {
        readonly get => m_capacity;
        set => SetCapacity(value);
    }

    public T this[int index]
    {
        get => Ptr[index];
        set => Ptr[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T ElementAt(int index) => ref Ptr[index];

    public NativeRawList(T* ptr, int length) : this()
    {
        Ptr = ptr;
        m_length = length;
        m_capacity = length;
    }

    public NativeRawList(int initialCapacity)
    {
        Ptr = null;
        m_length = 0;
        m_capacity = 0;
        SetCapacity(IshtarMath.max(initialCapacity, 1));
    }

    public static NativeRawList<T>* Create(int initialCapacity)
    {
        var listData = IshtarGC.AllocateImmortal<NativeRawList<T>>();
        *listData = new NativeRawList<T>(initialCapacity);
        return listData;
    }

    public static void Destroy(NativeRawList<T>* listData)
    {
        listData->Dispose();
        IshtarGC.FreeImmortal(listData);
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsCreated || m_length == 0;
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr != null;
    }

    public void Dispose()
    {
        if (!IsCreated)
            return;

        IshtarGC.FreeImmortal(Ptr);


        Ptr = null;
        m_length = 0;
        m_capacity = 0;
    }

    public void Clear() => m_length = 0;


    public void Resize(int length)
    {
        if (length > Capacity) SetCapacity(length);
        m_length = length;
    }

    void ResizeExact(int newCapacity)
    {
        newCapacity = IshtarMath.max(0, newCapacity);

        T* newPointer = null;

        var sizeOf = sizeof(T);

        if (newCapacity > 0)
        {
            newPointer = IshtarGC.AllocateImmortal<T>(sizeOf);

            if (Ptr != null && m_capacity > 0)
            {
                var itemsToCopy = IshtarMath.min(newCapacity, Capacity);
                var bytesToCopy = itemsToCopy * sizeOf;
                IshtarUnsafe.MemoryCopy(newPointer, Ptr, bytesToCopy);
            }
        }

        IshtarGC.FreeImmortal(Ptr);

        Ptr = newPointer;
        m_capacity = newCapacity;
        m_length = IshtarMath.min(m_length, newCapacity);
    }

    public void SetCapacity(int capacity)
    {
        var sizeOf = sizeof(T);
        var newCapacity = IshtarMath.max(capacity, 64 / sizeOf);
        newCapacity = IshtarMath.ceil_pow2(newCapacity);

        if (newCapacity == Capacity)
            return;

        ResizeExact(newCapacity);
    }

    public void TrimExcess()
    {
        if (Capacity != m_length) ResizeExact(m_length);
    }


    public void AddRangeNoResize(void* ptr, int count)
    {
        var sizeOf = sizeof(T);
        void* dst = (byte*)Ptr + m_length * sizeOf;
        IshtarUnsafe.MemoryCopy(dst, ptr, count * sizeOf);
        m_length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(in T value)
    {
        var idx = m_length;
        if (m_length < m_capacity)
        {
            Ptr[idx] = value;
            m_length++;
            return;
        }

        Resize(idx + 1);
        Ptr[idx] = value;
    }

    public void AddRange(void* ptr, int count)
    {
        var idx = m_length;

        if (m_length + count > Capacity)
        {
            Resize(m_length + count);
        }
        else
        {
            m_length += count;
        }

        var sizeOf = sizeof(T);
        void* dst = (byte*)Ptr + idx * sizeOf;
        IshtarUnsafe.MemoryCopy(dst, ptr, count * sizeOf);
    }

    public void AddRange(NativeRawList<T> list) => AddRange(list.Ptr, list.Length);

    public void InsertRangeWithBeginEnd(int begin, int end)
    {
        int items = end - begin;
        if (items < 1)
            return;

        var oldLength = m_length;

        if (m_length + items > Capacity)
        {
            Resize(m_length + items);
        }
        else
        {
            m_length += items;
        }

        var itemsToCopy = oldLength - begin;

        if (itemsToCopy < 1)
            return;

        var sizeOf = sizeof(T);
        var bytesToCopy = itemsToCopy * sizeOf;
        unsafe
        {
            byte* ptr = (byte*)Ptr;
            byte* dest = ptr + end * sizeOf;
            byte* src = ptr + begin * sizeOf;
            IshtarUnsafe.MoveMemory(dest, src, bytesToCopy);
        }
    }

    public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

    public void RemoveAtSwapBack(int index)
    {
        var copyFrom = m_length - 1;
        T* dst = Ptr + index;
        T* src = Ptr + copyFrom;
        (*dst) = (*src);
        m_length -= 1;
    }

    public void RemoveRangeSwapBack(int index, int count)
    {
        if (count <= 0)
            return;
        int copyFrom = IshtarMath.max(m_length - count, index + count);
        var sizeOf = sizeof(T);
        void* dst = (byte*)Ptr + index * sizeOf;
        void* src = (byte*)Ptr + copyFrom * sizeOf;
        IshtarUnsafe.MemoryCopy(dst, src, (m_length - copyFrom) * sizeOf);
        m_length -= count;
    }

    public void RemoveAt(int index)
    {
        T* dst = Ptr + index;
        T* src = dst + 1;
        m_length--;
        for (int i = index; i < m_length; i++) *dst++ = *src++;
    }

    public void RemoveRange(int index, int count)
    {
        if (count > 0)
        {
            int copyFrom = IshtarMath.min(index + count, m_length);
            var sizeOf = sizeof(T);
            void* dst = (byte*)Ptr + index * sizeOf;
            void* src = (byte*)Ptr + copyFrom * sizeOf;
            IshtarUnsafe.MemoryCopy(dst, src, (m_length - copyFrom) * sizeOf);
            m_length -= count;
        }
    }

    /// <summary>
    /// Returns an enumerator over the elements of the list.
    /// </summary>
    /// <returns>An enumerator over the elements of the list.</returns>
    public Enumerator GetEnumerator() => new() { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

    public struct Enumerator : IEnumerator<T>
    {
        internal T* m_Ptr;
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

        /// <summary>
        /// The current element.
        /// </summary>
        /// <value>The current element.</value>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Ptr[m_Index];
        }

        object IEnumerator.Current => Current;
    }
}
