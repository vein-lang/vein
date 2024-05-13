namespace ishtar.collections;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using runtime;
using vm;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct RawKVPair<TKey>
    where TKey : unmanaged, IEquatable<TKey>
{
    internal byte* Ptr;
    internal TKey* Keys;
    internal int* Next;
    internal int* Jar;

    internal int count;
    internal int capacity;
    internal int l2mg;
    internal int jarCapacity;
    internal int allocIdx;
    internal int FFIdx;
    internal int tSize;

    internal const int minCap = 1024;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int CalcCapacityCeilPow2(int cap)
    {
        cap = IshtarMath.max(IshtarMath.max(1, count), cap);
        var newCapacity = IshtarMath.max(cap, 1 << l2mg);
        var result = IshtarMath.ceil_pow2(newCapacity);

        return result;
    }

    internal static int GetJarSize(int capacity) => capacity * 2;

    internal readonly bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr != null;
    }

    internal readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsCreated || count == 0;
    }

    internal void Clear()
    {
        IshtarUnsafe.MemSet(Jar, 0xff, (ulong)jarCapacity * sizeof(int));
        IshtarUnsafe.MemSet(Next, 0xff, (ulong)capacity * sizeof(int));

        count = 0;
        FFIdx = -1;
        allocIdx = 0;
    }

    internal void Init(int cap, int tSizeVal, int mg)
    {
        count = 0;
        l2mg = (byte)(32 - IshtarMath.lzcnt(IshtarMath.max(1, mg) - 1));

        cap = CalcCapacityCeilPow2(cap);
        capacity = cap;
        jarCapacity = GetJarSize(cap);
        tSize = tSizeVal;

        var totalSize = CalcDataSize(cap, jarCapacity, tSizeVal, out int keyOff, out int nextOff, out int jarOff);


        Ptr = IshtarGC.AllocateImmortal<byte>(totalSize);
        Keys = (TKey*)(Ptr + keyOff);
        Next = (int*)(Ptr + nextOff);
        Jar = (int*)(Ptr + jarOff);

        Clear();
    }

    internal void Dispose()
    {
        IshtarGC.FreeImmortal(Ptr);
        Ptr = null;
        Keys = null;
        Next = null;
        Jar = null;
        count = 0;
        jarCapacity = 0;
    }

    internal static RawKVPair<TKey>* Alloc(int capacity, int tSizeVal, int mg)
    {
        var data = IshtarGC.AllocateImmortal<RawKVPair<TKey>>();
        data->Init(capacity, tSizeVal, mg);

        return data;
    }

    internal static void Free(RawKVPair<TKey>* data)
    {
        if (data == null)
            throw new InvalidOperationException("Hash based container has yet to be created or has been destroyed!");
        data->Dispose();
        IshtarGC.FreeImmortal(data);
    }

    internal void Resize(int newCapacity)
    {
        newCapacity = IshtarMath.max(newCapacity, count);
        var newBucketCapacity = IshtarMath.ceil_pow2(GetJarSize(newCapacity));

        if (capacity == newCapacity && jarCapacity == newBucketCapacity)
            return;

        ResizeHard(newCapacity, newBucketCapacity);
    }

    internal void ResizeHard(int newCap, int newJarCap)
    {
        int totalSize = CalcDataSize(newCap, newJarCap, tSize, out int keyOff, out int nextOff, out int jarOff);

        var oldPtr = Ptr;
        var oldKeys = Keys;
        var oldNext = Next;
        var oldJar = Jar;
        var oldJarCap = jarCapacity;

        Ptr = IshtarGC.AllocateImmortal<byte>(totalSize);
        Keys = (TKey*)(Ptr + keyOff);
        Next = (int*)(Ptr + nextOff);
        Jar = (int*)(Ptr + jarOff);
        capacity = newCap;
        jarCapacity = newJarCap;

        Clear();

        for (int i = 0, num = oldJarCap; i < num; ++i)
        for (int idx = oldJar[i]; idx != -1; idx = oldNext[idx])
        {
            var newIdx = TryAdd(oldKeys[idx]);
            IshtarUnsafe.MemoryCopy(Ptr + tSize * newIdx, oldPtr + tSize * idx, tSize);
        }

        IshtarGC.FreeImmortal(oldPtr);
    }

    internal void TrimExcess()
    {
        var cap = CalcCapacityCeilPow2(count);
        ResizeHard(cap, GetJarSize(cap));
    }

    internal static int CalcDataSize(int capacity, int jarCap, int tSizeVal, out int keyOff, out int nextOff, out int jarOff)
    {
        var sizeOfTKey = sizeof(TKey);
        var sizeOfInt = sizeof(int);

        var valuesSize = tSizeVal * capacity;
        var keysSize = sizeOfTKey * capacity;
        var nextSize = sizeOfInt * capacity;
        var bucketSize = sizeOfInt * jarCap;
        var totalSize = valuesSize + keysSize + nextSize + bucketSize;

        keyOff = 0 + valuesSize;
        nextOff = keyOff + keysSize;
        jarOff = nextOff + nextSize;

        return totalSize;
    }

    internal readonly int GetCount()
    {
        if (allocIdx <= 0)
            return 0;

        var numFree = 0;

        for (var freeIdx = FFIdx; freeIdx >= 0; freeIdx = Next[freeIdx]) ++numFree;

        return IshtarMath.min(capacity, allocIdx) - numFree;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetBucket(in TKey key) => (int)((uint)key.GetHashCode() & (jarCapacity - 1));

    internal int TryAdd(in TKey key)
    {
        if (Find(key) != -1) return -1;
        if (allocIdx >= capacity && FFIdx < 0)
        {
            int newCap = CalcCapacityCeilPow2(capacity + (1 << l2mg));
            Resize(newCap);
        }

        int idx = FFIdx;

        if (idx >= 0)
            FFIdx = Next[idx];
        else
            idx = allocIdx++;

        CheckIndexOutOfBounds(idx);

        IshtarUnsafe.WriteArrayElement((byte*)Keys, idx, key);
        var bucket = GetBucket(key);

        int* next = Next;
        next[idx] = Jar[bucket];
        Jar[bucket] = idx;
        count++;

        return idx;
    }

    internal int Find(TKey key)
    {
        if (allocIdx <= 0)
            return -1;

        var bucket = GetBucket(key);
        var entryIdx = Jar[bucket];

        if ((uint)entryIdx >= (uint)capacity)
            return -1;
        var nextPtrs = Next;
        while (!IshtarUnsafe.ReadArrayElement<TKey>((byte*)Keys, entryIdx).Equals(key))
        {
            entryIdx = nextPtrs[entryIdx];
            if ((uint)entryIdx >= (uint)capacity)
                return -1;
        }

        return entryIdx;

    }

    internal bool TryGetValue<TValue>(TKey key, out TValue item) where TValue : unmanaged
    {
        var idx = Find(key);

        if (-1 != idx)
        {
            item = IshtarUnsafe.ReadArrayElement<TValue>(Ptr, idx);
            return true;
        }

        item = default;
        return false;
    }

    internal int TryRemove(TKey key)
    {
        if (capacity == 0) return -1;
        var removed = 0;

        var bucket = GetBucket(key);

        var prevEntry = -1;
        var entryIdx = Jar[bucket];

        while (entryIdx >= 0 && entryIdx < capacity)
        {
            if (IshtarUnsafe.ReadArrayElement<TKey>((byte*)Keys, entryIdx).Equals(key))
            {
                ++removed;

                if (prevEntry < 0)
                    Jar[bucket] = Next[entryIdx];
                else
                    Next[prevEntry] = Next[entryIdx];

                int nextIdx = Next[entryIdx];
                Next[entryIdx] = FFIdx;
                FFIdx = entryIdx;
                break;
            }

            prevEntry = entryIdx;
            entryIdx = Next[entryIdx];
        }

        count -= removed;
        return 0 != removed ? removed : -1;

    }

    internal bool MoveNextSearch(ref int bucketIndex, ref int nextIndex, out int index)
    {
        for (int i = bucketIndex, num = jarCapacity; i < num; ++i)
        {
            var idx = Jar[i];

            if (idx == -1) continue;
            index = idx;
            bucketIndex = i + 1;
            nextIndex = Next[idx];

            return true;
        }

        index = -1;
        bucketIndex = jarCapacity;
        nextIndex = -1;
        return false;
    }

    internal bool MoveNext(ref int bucketIndex, ref int nextIndex, out int index)
    {
        if (nextIndex == -1) return MoveNextSearch(ref bucketIndex, ref nextIndex, out index);
        index = nextIndex;
        nextIndex = Next[nextIndex];
        return true;

    }
    
    void CheckIndexOutOfBounds(int idx)
    {
        if ((uint)idx >= (uint)capacity)
            throw new InvalidOperationException($"Internal Dict error. idx {idx}");
    }
}
