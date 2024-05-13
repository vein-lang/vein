namespace ishtar.allocators;

using runtime;

public sealed unsafe class IshtarAllocatorPool(GCLayout? layout) : IIshtarAllocatorPool
{
    internal readonly Dictionary<nint, IIshtarAllocator> _allocators = new();


    private IIshtarAllocator GetAllocator(CallFrame frame)
    {
        if (layout is not null)
            return new GCLayoutAllocator(layout);

        if (frame.vm.Config.UseDebugAllocator)
            return new DebugManagedAllocator();

        var allocator = 
#if WINDOWS
            new NativeMemory_WindowsAllocator();
#elif LINUX
            new NullAllocator();
#elif OSX
#warning THIS OS CUSTOM ALLOCATOR IS NOT IMPLEMENTED
            new NullAllocator();
#else
#warning THIS OS CUSTOM ALLOCATOR IS NOT IMPLEMENTED
            new NullAllocator();
#endif

        return allocator;
    }


    public IIshtarAllocator Rent<T>(out T* output, AllocationKind kind, CallFrame frame) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T), kind, frame);

        if (allocator is IIshtarAllocatorIdentifier identifier)
            identifier.SetId((nint)output);
        _allocators[allocator.Id] = allocator;

        return allocator;
    }

    public IIshtarAllocator RentArray<T>(out T* output, int size, CallFrame frame) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T) * size, AllocationKind.reference, frame);

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