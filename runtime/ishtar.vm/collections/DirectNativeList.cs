namespace ishtar.collections;

using System;
using System.Runtime.InteropServices;
using runtime;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DirectNativeList<T> : IDisposable, IUnsafeNativeList<T>
    where T : unmanaged
{
#if USE_MANAGED_COLLECTIONS

    public static readonly Dictionary<uint, List<nint>> _direct_list_cache = new Dictionary<uint, List<IntPtr>>()
    {
        { 0, new List<IntPtr>() }
    };

    private uint _ref_ptr;
    private uint _ctor_called;
    private DirectNativeList<T>* _selfRef;
    private DirectNativeList<T>* _weak_Ref;
#else
    internal NativeRawList<nint>* _ref;
#endif


    public static void finalizer(nint p, nint _)
    {
        var w = (DirectNativeList<T>*)p;

        Console.WriteLine($"Finalized DirectNativeList<{typeof(T).Name}>");
    }

#if USE_MANAGED_COLLECTIONS
    public List<nint>.Enumerator GetEnumerator() => _direct_list_cache[this._ref_ptr].GetEnumerator();
#else
    public NativeRawList<nint>.Enumerator GetEnumerator() => _ref->GetEnumerator();
#endif


    public DirectNativeList(DirectNativeList<T>* selfRef) 
    {
        this = default;
#if !USE_MANAGED_COLLECTIONS
        _ref = NativeRawList<nint>.Create(initialCapacity);
#else
        _direct_list_cache[this._ref_ptr = (uint)(_direct_list_cache.Count + 1)] = new List<IntPtr>(4);
#endif
        _ctor_called = 1488;
        _selfRef = selfRef;
    }

    public DirectNativeList(int initialCapacity, DirectNativeList<T>* selfRef)
    {
        this = default;
#if !USE_MANAGED_COLLECTIONS
        _ref = NativeRawList<nint>.Create(initialCapacity);
#else
        _direct_list_cache[this._ref_ptr = (uint)(_direct_list_cache.Count + 1)] = new List<IntPtr>(initialCapacity);
#endif
        _ctor_called = 1488;
        _selfRef = selfRef;
    }
    private static nint** _root_refs;
    private static nint _root_refs_offset;

    internal static DirectNativeList<T>* New(int initialCapacity)
    {
        if (_root_refs is null)
        {
            _root_refs = (nint**)IshtarGC.AllocateImmortalRoot<nint>(1024);
        }
        var lst = IshtarGC.AllocateImmortal<DirectNativeList<T>>();


        _root_refs[_root_refs_offset] = (nint*)lst;
        _root_refs_offset++;

        *lst = new DirectNativeList<T>(initialCapacity, lst);

        BoehmGCLayout.register_finalizer_no_order2(lst, finalizer);

        return lst;
    }
#if USE_MANAGED_COLLECTIONS
    public T* this[int index]
    {
        get => (T*)_direct_list_cache[this._ref_ptr][index];
        set => _direct_list_cache[this._ref_ptr][index] = (nint)value;
    }

    public int Size
    {
        get => _direct_list_cache[this._ref_ptr].Count;
        set {}
    }
    public T* ElementAt(int index) => (T*)_direct_list_cache[this._ref_ptr].ElementAt(index);

    public int Length => _direct_list_cache[this._ref_ptr].Count;


    public void Assert()
    {
        if (_ctor_called == 0)
            throw new Exception($"ctor not called but address used");
        if (_ctor_called != 1488)
            throw new Exception($"ctor not called and address bad");
    }

    public int Capacity
    {
        readonly get => _direct_list_cache[this._ref_ptr].Capacity;
        set => _direct_list_cache[this._ref_ptr].Capacity = value;
    }

    public void Add(in T* value) => _direct_list_cache[this._ref_ptr].Add((nint)value);
    public void AddIfNotExist(in T* value, UnsafePredicate<T> exist)
    {
        if (!Any(exist))
            _direct_list_cache[this._ref_ptr].Add((nint)value);
    }

    public void ReplaceIfExist(in T* value, UnsafePredicate<T> exist)
    {
        if (!Any(exist))
            _direct_list_cache[this._ref_ptr].Add((nint)value);
        else
        {
            var r = _direct_list_cache[this._ref_ptr].FirstOrDefault(x => exist((T*)x));
            _direct_list_cache[this._ref_ptr].Remove(r);
            _direct_list_cache[this._ref_ptr].Add((nint)value);
        }
    }

    public void ReplaceTo(in T* from, in T* to)
    {
        _direct_list_cache[this._ref_ptr].Remove((nint)from);
        _direct_list_cache[this._ref_ptr].Add((nint)to);
    }

    public T* Get(in int index) => (T*)(_direct_list_cache[this._ref_ptr])[index];

    public void AddRange(DirectNativeList<T> array) => _direct_list_cache[this._ref_ptr].AddRange(_direct_list_cache[array._ref_ptr]);

    public void AddRange(DirectNativeList<T>* array) => _direct_list_cache[this._ref_ptr].AddRange(_direct_list_cache[array->_ref_ptr]);
    
    public void RemoveAt(int index) => _direct_list_cache[this._ref_ptr].RemoveAt(index);

    public void RemoveRange(int index, int count) => _direct_list_cache[this._ref_ptr].RemoveRange(index, count);

    public readonly bool IsEmpty => !_direct_list_cache.ContainsKey(_ref_ptr) || _direct_list_cache[this._ref_ptr].Count == 0;

    public readonly bool IsCreated => _direct_list_cache[this._ref_ptr] != null;

    public void Dispose()
    {
    }

    public void Clear() => _direct_list_cache[this._ref_ptr].Clear();

    public void Resize(int length) {}

    public void SetCapacity(int capacity) => _direct_list_cache[this._ref_ptr].Capacity = capacity;

    public void TrimExcess() => _direct_list_cache[this._ref_ptr].TrimExcess();


    public bool Any(UnsafePredicate<T> predicate) => _direct_list_cache[this._ref_ptr].Where(x => x != 0).Any(x => predicate((T*)x));

    public T* FirstOrNull(UnsafePredicate<T> predicate)
    {
        if (!_direct_list_cache.ContainsKey(this._ref_ptr)) Console.WriteLine($"list is null suka");

        return (T*)_direct_list_cache[this._ref_ptr].FirstOrDefault(x => predicate((T*)x));
    }

    public void ForEach(UnsafeVoidActor<T> actor) => _direct_list_cache[this._ref_ptr].ForEach(x => actor((T*)x));

#else
    
    public void ReplaceIfExist(in T* value, UnsafePredicate<T> exist)
    {
        if (!Any(exist))
            _ref->Add((nint)value);
        else
        {
            var r = FirstOrNullIndex(exist);
            _ref->RemoveAt(r);
            _ref->Add((nint)value);
        }
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
    
    public void Add(in T* value) => _ref->Add((nint)value);
    public void AddIfNotExist(in T* value, UnsafePredicate<T> exist)
    {
        if (!Any(exist))
            _ref->Add((nint)value);
    }

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


    public int FirstOrNullIndex(UnsafePredicate<T> predicate)
    {
        using var enumerator = _ref->GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (predicate((T*)enumerator.Current))
                return enumerator.m_Index;
        }

        return -1;
    }

    public void ForEach(UnsafeVoidActor<T> actor)
    {
        using var enumerator = _ref->GetEnumerator();

        while (enumerator.MoveNext())
            actor((T*)enumerator.Current);
    }
#endif
}
