namespace ishtar.allocators;

using runtime;

public sealed unsafe class GCLayoutAllocator(GCLayout layout) : IIshtarAllocator
{
    public long TotalSize { get; private set; }
    public nint Id { get; private set; }
    public void* AllocZeroed(ulong size, AllocationKind kind, CallFrame frame)
    {
        TotalSize += (long)size;
        return Alloc((nint)size, kind, frame);
    }

    public void* AllocZeroed(long size, AllocationKind kind, CallFrame frame)
    {
        TotalSize += size;
        return Alloc((nint)size, kind, frame);
    }

    public void* AllocZeroed(UIntPtr size, AllocationKind kind, CallFrame frame)
    {
        TotalSize += (long)size;
        return Alloc((nint)size, kind, frame);
    }

    public void* AllocZeroed(IntPtr size, AllocationKind kind, CallFrame frame)
    {
        TotalSize += size;
        return Alloc(size, kind, frame);
    }

    public void* AllocZeroed(int size, AllocationKind kind, CallFrame frame)
    {
        TotalSize += size;
        return Alloc(size, kind, frame);
    }

    private void* Alloc(nint size, AllocationKind kind, CallFrame frame)
    {
        //frame.assert(size != 0, WNE.STATE_CORRUPT, "Allocation is not allowed zero size");

        return kind switch
        {
            AllocationKind.no_reference => layout.alloc_atomic((uint)size),
            AllocationKind.reference => layout.alloc((uint)size),
            _ => throw null // TODO
        };
    }



    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;


    void IIshtarAllocatorDisposer.FreeAll()
    {
        return; // no support when gc layout use
    }
}
