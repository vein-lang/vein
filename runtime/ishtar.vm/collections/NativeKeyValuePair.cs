namespace ishtar.collections;

using System;
using System.Runtime.CompilerServices;

public unsafe struct NativeKeyValuePair<K, V> where K : unmanaged, IEquatable<K>  where V : unmanaged
{
    internal RawKVPair<K>* m_Data;
    internal int m_Index;
    internal int m_Next;

    public static NativeKeyValuePair<K, V> Null => new() { m_Index = -1 };

    public K Key => m_Index != -1 ? m_Data->Keys[m_Index] : default;
    public ref V Value => ref Unsafe.AsRef<V>(m_Data->Ptr + sizeof(V) * m_Index);

    public bool GetKeyValue(out K key, out V value)
    {
        if (m_Index != -1)
        {
            key = m_Data->Keys[m_Index];
            value = IshtarUnsafe.ReadArrayElement<V>(m_Data->Ptr, m_Index);
            return true;
        }

        key = default;
        value = default;
        return false;
    }
}
