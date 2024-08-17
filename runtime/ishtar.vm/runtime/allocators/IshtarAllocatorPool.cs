namespace ishtar.allocators;

using runtime;
using runtime.gc;

public sealed unsafe class IshtarAllocatorPool(GCLayout? layout) : IIshtarAllocatorPool
{
    internal readonly Dictionary<nint, IIshtarAllocator> _allocators = new();


    private IIshtarAllocator GetAllocator(CallFrame* frame)
    {
        if (layout is not null)
            return new GCLayoutAllocator(layout);

        if (frame->vm->@ref->Config.UseDebugAllocator)
            return new DebugManagedAllocator();

        throw new NotImplementedException();
    }


    public IIshtarAllocator Rent<T>(out T* output, AllocationKind kind, CallFrame* frame) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T), kind, frame);

        if (allocator is IIshtarAllocatorIdentifier identifier)
            identifier.SetId((nint)output);
        _allocators[allocator.Id] = allocator;

        return allocator;
    }

    public IIshtarAllocator RentArray<T>(out T* output, int size, CallFrame* frame, AllocationKind kind = AllocationKind.reference) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T) * size, kind, frame);

        if (allocator is IIshtarAllocatorIdentifier identifier)
            identifier.SetId((nint)output);
        _allocators[allocator.Id] = allocator;

        return allocator;
    }

    public long Return(nint p)
    {
        var allocator = _allocators[p];

        allocator.FreeAll();

        return allocator.TotalSize;
    }

    public long Return(void* p) => Return((nint)p);
}
