namespace ishtar.collections;

using System;
using runtime;
using static ishtar.PInvokeInfo;

public unsafe struct DirectNativeList<T> : IDisposable, IUnsafeNativeList<T>
    where T : unmanaged
{
    internal NativeRawList<nint>* _ref;

    public DirectNativeList() : this(1) { }

    public DirectNativeList(int initialCapacity)
    {
        this = default;
        Initialize(initialCapacity);
    }

    internal void Initialize(int initialCapacity)
        => _ref = NativeRawList<nint>.Create(initialCapacity);

    internal static DirectNativeList<T>* New(int initialCapacity)
    {
        var lst = IshtarGC.AllocateImmortal<DirectNativeList<T>>();
        *lst = new DirectNativeList<T>();
        lst->Initialize(initialCapacity);
        return lst;
    }

    public T* this[int index]
    {
        get => (T*)(*_ref)[index];
        set => (*_ref)[index] = (nint)value;
    }

    public int Size
    {
        get => _ref->Size;
        set => _ref->Size = value;
    }

    public T* ElementAt(int index) => (T*)_ref->ElementAt(index);

    public int Length
    {
        readonly get => _ref->Length;
        set => _ref->Resize(value);
    }

    public int Capacity
    {
        readonly get => _ref->Capacity;
        set => _ref->Capacity = value;
    }

    public NativeRawList<nint>* GetUnsafeList() => _ref;

    public void Add(in T* value) => _ref->Add((nint)value);

    public T* Get(in int index) => (T*)(*_ref)[index];

    public void AddRange(DirectNativeList<T> array) => AddRange(array._ref->Ptr, array.Length);
    public void AddRange(DirectNativeList<T>* array) => AddRange(array->_ref->Ptr, array->Length);

    public void AddRange(void* ptr, int count) => _ref->AddRange(ptr, count);

    public void InsertRangeWithBeginEnd(int begin, int end) => _ref->InsertRangeWithBeginEnd(begin, end);
    public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

    public void RemoveAtSwapBack(int index) => _ref->RemoveAtSwapBack(index);

    public void RemoveRangeSwapBack(int index, int count) => _ref->RemoveRangeSwapBack(index, count);

    public void RemoveAt(int index) => _ref->RemoveAt(index);

    public void RemoveRange(int index, int count) => _ref->RemoveRange(index, count);

    public readonly bool IsEmpty => _ref == null || _ref->Length == 0;

    public readonly bool IsCreated => _ref != null;

    public void Dispose()
    {
        if (!IsCreated)
            return;
        NativeRawList<nint>.Destroy(_ref);
        _ref = null;
    }

    public void Clear() => _ref->Clear();
    
    public void Resize(int length) => _ref->Resize(length);

    public void SetCapacity(int capacity) => _ref->SetCapacity(capacity);

    public void TrimExcess() => _ref->TrimExcess();


    public bool Any(UnsafePredicate<T> predicate)
    {
        using var enumerator = _ref->GetEnumerator();

        var flag = false;
        while (enumerator.MoveNext())
            flag |= predicate((T*)enumerator.Current);

        return flag;
    }

    public T* FirstOrNull(UnsafePredicate<T> predicate)
    {
        using var enumerator = _ref->GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (predicate((T*)enumerator.Current))
                return (T*)enumerator.Current;
        }

        return null;
    }

    public void ForEach(UnsafeVoidActor<T> actor)
    {
        using var enumerator = _ref->GetEnumerator();

        while (enumerator.MoveNext())
            actor((T*)enumerator.Current);
    }

    //public void Mutate<E>(UnsafeFuncActor<T, E> actor) where E : unmanaged
    //{
    //    using var enumerator = _nativeData->GetEnumerator();


    //    var p = UnsafeNativeList<E>.New(Length);

    //    while (enumerator.MoveNext())
    //        p.Add(actor((T*)enumerator.Current));
    //}
}
