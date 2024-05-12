namespace ishtar.collections;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using runtime;

public unsafe struct UnsafeDictionary<K, V>
    : IDisposable , IEnumerable<NativeKeyValuePair<K, V>>
    where K : unmanaged, IEquatable<K>
    where V : unmanaged
{
    internal RawKVPair<K> _ref;

    public UnsafeDictionary(int initCap)
    {
        _ref = default;
        _ref.Init(initCap, sizeof(V), RawKVPair<K>.minCap);
    }

    public static UnsafeDictionary<K, V>* New(int capacity) 
    {
        var im =  IshtarGC.AllocateImmortal<UnsafeDictionary<K, V>>();

        *im = new UnsafeDictionary<K, V>(capacity);

        return im;
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

    public bool TryAdd(K key, V item)
    {
        var idx = _ref.TryAdd(key);
        if (-1 == idx) return false;
        IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, item);
        return true;

    }

    public void Add(K key, V item)
    {
        var result = TryAdd(key, item);

        if (!result) ThrowKeyAlreadyAdded(key);
    }

    public bool Remove(K key) => -1 != _ref.TryRemove(key);

    public bool TryGetValue(K key, out V item) => _ref.TryGetValue(key, out item);

    public bool ContainsKey(K key) => -1 != _ref.Find(key);

    public void TrimExcess() => _ref.TrimExcess();

    public V this[K key]
    {
        get
        {
            if (!_ref.TryGetValue(key, out V result)) ThrowKeyNotPresent(key);
            return result;
        }

        set
        {
            var idx = _ref.Find(key);
            if (-1 != idx)
            {
                IshtarUnsafe.WriteArrayElement(_ref.Ptr, idx, value);
                return;
            }

            TryAdd(key, value);
        }
    }


    [Conditional("DEBUG")]
    void ThrowKeyNotPresent(K key) => throw new ArgumentException($"Key {key} is not exist");

    [Conditional("DEBUG")]
    void ThrowKeyAlreadyAdded(K key) => throw new ArgumentException($"Item with same key has already added: {key}");
    public IEnumerator<NativeKeyValuePair<K, V>> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}