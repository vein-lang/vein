namespace ishtar;

public readonly unsafe struct SmartPointer<T>(ushort size, CallFrame frame,
    delegate*<CallFrame, int, T*> allocator,
    delegate*<CallFrame, T*, int, void> free) : IDisposable where T : unmanaged
{
    public readonly T* Ref = allocator(frame, size);
    public readonly ushort size = size;

    public void Dispose()
    {
        if (!IsNull()) free(frame, Ref, size);
    }

    public ref T this[int index] => ref Ref[index];
    public ref T this[uint index] => ref Ref[index];


    public bool IsNull() => Ref is null;
}
