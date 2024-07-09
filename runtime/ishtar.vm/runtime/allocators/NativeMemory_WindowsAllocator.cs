namespace ishtar.allocators;

public sealed unsafe class NativeMemory_WindowsAllocator : IIshtarAllocator
{
    public List<nint> Memory { get; } = new();
    public long TotalSize { get; private set; }
    public nint Id { get; private set; }


    private void* Allocate(long size, CallFrame* frame)
    {
        //frame.assert(size != 0, WNE.STATE_CORRUPT, "Allocation is not allowed zero size");
        TotalSize += size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    public void* AllocZeroed(ulong size, AllocationKind kind, CallFrame* frame)
        => Allocate((long)size, frame);

    public void* AllocZeroed(long size, AllocationKind kind, CallFrame* frame)
        => Allocate(size, frame);

    public void* AllocZeroed(UIntPtr size, AllocationKind kind, CallFrame* frame)
        => Allocate((long)size, frame);

    public void* AllocZeroed(IntPtr size, AllocationKind kind, CallFrame* frame)
        => Allocate(size, frame);

    public void* AllocZeroed(int size, AllocationKind kind, CallFrame* frame)
        => Allocate(size, frame);

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;

    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in Memory)
            NativeMemory.Free((void*)p);
        Memory.Clear();
    }
}
