namespace ishtar.collections;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using runtime;

public unsafe struct DirectNativeDictionary<TKey, TValue> : IDisposable, IEnumerable<NativeKeyValuePair<TKey, nint>>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal RawKVPair<TKey> _ref;

    public DirectNativeDictionary(int initCap)
    {
        _ref = default;
        _ref.Init(initCap, sizeof(nint), RawKVPair<TKey>.minCap);
    }

    public static DirectNativeDictionary<TKey, TValue>* New()
    {
        var @ref = IshtarGC.AllocateImmortal<DirectNativeDictionary<TKey, TValue>>();

        *@ref = new DirectNativeDictionary<TKey, TValue>(32);

        return @ref;
    }

    public void Dispose()
    {
        if (!IsCreated)
            return;

        _ref.Dispose();
    }

    public readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.IsCreated;
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.IsEmpty;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ref.count;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => _ref.capacity;
        set => _ref.Resize(value);
    }

    public void Clear() => _ref.Clear();

    public bool TryAdd(TKey key, TValue* item)
    {
        var idx = _ref.TryAdd(key);
        if (-1 != idx)
        {
            IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, (nint)item);
            return true;
        }

        return false;
    }

    public void Add(TKey key, TValue* item)
    {
        var result = TryAdd(key, item);

        if (!result) ThrowKeyAlreadyAdded(key);
    }

    public bool Remove(TKey key) => -1 != _ref.TryRemove(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue* item)
    {
        var result = _ref.TryGetValue(key, out nint @ref);
        item = (TValue*)@ref;
        return result;
    }

    public bool ContainsKey(TKey key) => -1 != _ref.Find(key);

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
            var idx = _ref.Find(key);
            if (-1 != idx)
            {
                IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, (nint)value);
                return;
            }

            TryAdd(key, value);
        }
    }


    [Conditional("DEBUG")]
    void ThrowKeyNotPresent(TKey key) => throw new ArgumentException($"Key: {key} is not present.");

    [Conditional("DEBUG")]
    void ThrowKeyAlreadyAdded(TKey key) => throw new ArgumentException($"An item with the same key has already been added: {key}");
    public IEnumerator<NativeKeyValuePair<TKey, nint>> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}