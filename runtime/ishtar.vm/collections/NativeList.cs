namespace ishtar.collections;

using System.Collections;

public unsafe struct NativeList<T>
    : IDisposable
        , INativeList<T>
        , IEnumerable<T>
    where T : unmanaged
{
    internal NativeRawList<T>* m_ListData;

    public NativeList()
        : this(1)
    {
    }

    public NativeList(int initialCapacity)
    {
        this = default;
        Initialize(initialCapacity);
    }

    internal void Initialize(int initialCapacity)
        => m_ListData = NativeRawList<T>.Create(initialCapacity);
    internal static NativeList<T> New(int initialCapacity)
    {
        var lst = new NativeList<T>();
        lst.Initialize(initialCapacity);
        return lst;
    }

    public T this[int index]
    {
        get => (*m_ListData)[index];
        set => (*m_ListData)[index] = value;
    }

    public int Size
    {
        get => m_ListData->Size;
        set => m_ListData->Size = value;
    }

    public ref T ElementAt(int index) => ref m_ListData->ElementAt(index);

    public int Length
    {
        readonly get => m_ListData->Length;
        set => m_ListData->Resize(value);
    }

    public int Capacity
    {
        readonly get => m_ListData->Capacity;
        set => m_ListData->Capacity = value;
    }

    public NativeRawList<T>* GetUnsafeList() => m_ListData;

    public void Add(in T value) => m_ListData->Add(in value);

    //public void AddRange(NativeArray<T> array) => AddRange(array.GetUnsafeReadOnlyPtr(), array.Length);

    public void AddRange(void* ptr, int count) => m_ListData->AddRange(ptr, count);

    public void InsertRangeWithBeginEnd(int begin, int end) => m_ListData->InsertRangeWithBeginEnd(begin, end);
    public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

    public void RemoveAtSwapBack(int index) => m_ListData->RemoveAtSwapBack(index);

    public void RemoveRangeSwapBack(int index, int count) => m_ListData->RemoveRangeSwapBack(index, count);

    public void RemoveAt(int index) => m_ListData->RemoveAt(index);

    public void RemoveRange(int index, int count) => m_ListData->RemoveRange(index, count);

    public readonly bool IsEmpty => m_ListData == null || m_ListData->Length == 0;

    public readonly bool IsCreated => m_ListData != null;

    public void Dispose()
    {
        if (!IsCreated)
            return;
        NativeRawList<T>.Destroy(m_ListData);
        m_ListData = null;
    }

    public void Clear() => m_ListData->Clear();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

    public void Resize(int length) => m_ListData->Resize(length);

    public void SetCapacity(int capacity) => m_ListData->SetCapacity(capacity);

    public void TrimExcess() => m_ListData->TrimExcess();
}