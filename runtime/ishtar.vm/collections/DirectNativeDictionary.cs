namespace ishtar.collections;

using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using runtime;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DirectNativeDictionary<TKey, TValue> : IDisposable, IEnumerable<NativeKeyValuePair<TKey, nint>>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    private readonly bool _enabledDebug;


    private static nint** _root_refs;
    private static nint _root_refs_offset;

#if USE_MANAGED_COLLECTIONS
    private void* _ref_ptr;
    internal Dictionary<TKey, nint> _ref
    {
        get => IshtarUnsafe.AsRef<Dictionary<TKey, nint>>(_ref_ptr);
        set => _ref_ptr = IshtarUnsafe.AsPointer(ref value);
    }
#else
    internal RawKVPair<TKey> _ref;
#endif
    public DirectNativeDictionary(int initCap, bool enabledDebug)
    {
        _enabledDebug = enabledDebug;
#if USE_MANAGED_COLLECTIONS
        _ref = new Dictionary<TKey, nint>(initCap);
#else
        _ref = default;
        _ref.Init(initCap, sizeof(nint), RawKVPair<TKey>.minCap);
#endif
    }

    public static DirectNativeDictionary<TKey, TValue>* New(bool enabledDebug = true)
    {
        if (_root_refs is null)
        {
            _root_refs = (nint**)IshtarGC.AllocateImmortalRoot<nint>(1024);
        }

        var @ref = IshtarGC.AllocateImmortal<DirectNativeDictionary<TKey, TValue>>();

        *@ref = new DirectNativeDictionary<TKey, TValue>(32, enabledDebug);

        _root_refs[_root_refs_offset] = (nint*)@ref;
        _root_refs_offset++;

        return @ref;
    }

    public void Dispose()
    {
        if (!IsCreated)
            return;
#if USE_MANAGED_COLLECTIONS

#else
    _ref.Dispose();
#endif
    }

    public readonly bool IsCreated
    {
#if USE_MANAGED_COLLECTIONS
        get => true;
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.IsCreated;
#endif
    }

    public readonly bool IsEmpty
    {
#if USE_MANAGED_COLLECTIONS
        get => _ref.Count == 0;
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.IsEmpty;
#endif
    }

    public readonly int Count
    {
#if USE_MANAGED_COLLECTIONS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.Count;
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.count;
#endif
    }

    public int Capacity
    {
#if USE_MANAGED_COLLECTIONS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.Count + 10;
        set => _ref.EnsureCapacity(value);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _ref.capacity;
        set => _ref.Resize(value);
#endif
    }

    public void Clear() => _ref.Clear();

    public bool TryAdd(TKey key, TValue* item)
    {
#if USE_MANAGED_COLLECTIONS
        return _ref.TryAdd(key, (nint)item);
#else
        var idx = _ref.TryAdd(key);
        if (-1 != idx)
        {
            IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, (nint)item);
            return true;
        }

        return false;
#endif
    }

    public void Add(TKey key, TValue* item)
    {
        var result = TryAdd(key, item);

        if (!result) ThrowKeyAlreadyAdded(key);
    }

    public bool Remove(TKey key)
    {
#if USE_MANAGED_COLLECTIONS
        return _ref.Remove(key);
#else
        return -1 != _ref.TryRemove(key);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue* item)
    {
        var result = _ref.TryGetValue(key, out nint @ref);
        item = (TValue*)@ref;
        return result;
    }

    public bool ContainsKey(TKey key)
    {
#if USE_MANAGED_COLLECTIONS
        return _ref.ContainsKey(key);
#else
        return -1 != _ref.Find(key);
#endif
    }

    public void TrimExcess() => _ref.TrimExcess();

    public TValue* this[TKey key]
    {
        get
        {
            if (!_ref.TryGetValue(key, out nint result)) ThrowKeyNotPresent(key);
            return (TValue*)result;
        }

        set
        {
#if USE_MANAGED_COLLECTIONS
            _ref.Add(key, (nint)value);
#else
            var idx = _ref.Find(key);
            if (-1 != idx)
            {
                IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, (nint)value);
                return;
            }

            TryAdd(key, value);
#endif
        }
    }


    public void ForEach(Action<KeyValuePair<TKey, nint>> filter)
    {
        foreach (var nint in _ref) filter(nint);
    }

    [Conditional("DEBUG")]
    void ThrowKeyNotPresent(TKey key) => throw new ArgumentException($"Key: {key} is not present.");

    [Conditional("DEBUG")]
    void ThrowKeyAlreadyAdded(TKey key) => throw new ArgumentException($"An item with the same key has already been added: {key}");
    public IEnumerator<NativeKeyValuePair<TKey, nint>> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
