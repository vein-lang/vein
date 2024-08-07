namespace ishtar.allocators;

using System.Reflection.Metadata;

public sealed unsafe class DebugManagedAllocator : IIshtarAllocator
{
    private readonly List<ManagedMemHandle> handles = new();

    public long TotalSize { get; private set; }
    public nint Id { get; private set; }



    private void* Alloc(nint size, CallFrame* frame)
    {
        frame->assert(size != 0, WNE.STATE_CORRUPT, "Allocation is not allowed zero size");

        var bytes = new byte[size];

        var handler = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        var p = (void*)handler.AddrOfPinnedObject();

        handles.Add(new(size, handler, (nint)p));

        return p;
    }


    internal record struct ManagedMemHandle(nint size, GCHandle handler, nint originalAddr);

    public void* AllocZeroed(ulong size, AllocationKind kind, CallFrame* frame)
    {
        TotalSize += (long)size;
        return Alloc((nint)size, frame);
    }

    public void* AllocZeroed(long size, AllocationKind kind, CallFrame* frame)
    {
        TotalSize += size;
        return Alloc((nint)size, frame);
    }

    public void* AllocZeroed(UIntPtr size, AllocationKind kind, CallFrame* frame)
    {
        TotalSize += (long)size;
        return Alloc((nint)size, frame);
    }

    public void* AllocZeroed(IntPtr size, AllocationKind kind, CallFrame* frame)
    {
        TotalSize += size;
        return Alloc(size, frame);
    }

    public void* AllocZeroed(int size, AllocationKind kind, CallFrame* frame)
    {
        TotalSize += size;
        return Alloc(size, frame);
    }

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;


    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in handles)
        {
            if (!p.handler.IsAllocated)
                throw new AccessViolationException("Handler is not allocated");

            var targetBytes = (byte[])p.handler.Target;

            if (targetBytes.Length != p.size)
                throw new AccessViolationException("Unequal size of memory trying dispose");

            p.handler.Free();
        }
        handles.Clear();
    }
}
