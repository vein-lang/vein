namespace ishtar.allocators;

public sealed unsafe class IshtarAllocatorPool : IIshtarAllocatorPool
{
    internal readonly Dictionary<nint, IIshtarAllocator> _allocators = new();


    private IIshtarAllocator GetAllocator(CallFrame frame)
    {
        if (frame.vm.Config.UseDebugAllocator)
            return new DebugManagedAllocator();

        var allocator = 
#if WINDOWS
            new NativeMemory_WindowsAllocator();
#elif LINUX
            new NullAllocator();
#elif OSX
            new NullAllocator();
#else
            new NullAllocator();
#endif

        return allocator;
    }


    public IIshtarAllocator Rent<T>(out T* output, CallFrame frame) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T), frame);

        if (allocator is IIshtarAllocatorIdentifier identifier)
            identifier.SetId((nint)output);
        _allocators[allocator.Id] = allocator;

        return allocator;
    }

    public IIshtarAllocator RentArray<T>(out T* output, int size, CallFrame frame) where T : unmanaged
    {
        var allocator = GetAllocator(frame);

        output = (T*)allocator.AllocZeroed(sizeof(T) * size, frame);

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
