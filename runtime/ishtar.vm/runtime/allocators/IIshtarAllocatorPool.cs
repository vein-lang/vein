namespace ishtar.allocators;

using System.Runtime.InteropServices;

public interface IIshtarAllocatorIdentifier
{
    void SetId(nint id);
}

public unsafe interface IIshtarAllocatorDisposer
{
    void FreeAll();
}

public unsafe interface IIshtarAllocator : IIshtarAllocatorIdentifier, IIshtarAllocatorDisposer
{
    List<nint> Memory { get; }
    long TotalSize { get; }
    nint Id { get; }

    void* AllocZeroed(ulong size);
    void* AllocZeroed(long size);
    void* AllocZeroed(nuint size);
    void* AllocZeroed(nint size);
    void* AllocZeroed(int size);
}


public sealed unsafe class WindowsAllocator : IIshtarAllocator
{
    public List<nint> Memory { get; } = new();
    public long TotalSize { get; private set; }
    public nint Id { get; private set; }


    public void* AllocZeroed(ulong size)
    {
        TotalSize += (long)size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    public void* AllocZeroed(long size)
    {
        TotalSize += (long)size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    public void* AllocZeroed(UIntPtr size)
    {
        TotalSize += (long)size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    public void* AllocZeroed(IntPtr size)
    {
        TotalSize += (long)size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    public void* AllocZeroed(int size)
    {
        TotalSize += (long)size;
        var p = NativeMemory.AllocZeroed((nuint)(size));
        Memory.Add((nint)p);
        return p;
    }

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;

    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in Memory) NativeMemory.Free((void*)p);
        Memory.Clear();
    }
}

public sealed unsafe class NullAllocator : IIshtarAllocator
{
    public List<IntPtr> Memory { get; }
    public long TotalSize { get; }
    public IntPtr Id { get; }

    public void* AllocZeroed(ulong size) => throw new NotImplementedException();

    public void* AllocZeroed(long size) => throw new NotImplementedException();

    public void* AllocZeroed(UIntPtr size) => throw new NotImplementedException();

    public void* AllocZeroed(IntPtr size) => throw new NotImplementedException();

    public void* AllocZeroed(int size) => throw new NotImplementedException();

    public void SetId(IntPtr id) => throw new NotImplementedException();
    public void FreeAll() => throw new NotImplementedException();
}



public unsafe interface IIshtarAllocatorPool
{
    IIshtarAllocator Rent<T>(out T* output) where T : unmanaged;
    IIshtarAllocator RentArray<T>(out T* output, int size) where T : unmanaged;
    long Return(nint p);
    long Return(void* p);
}

public sealed unsafe class IshtarAllocatorPool : IIshtarAllocatorPool
{
    private readonly Dictionary<nint, IIshtarAllocator> _allocators = new();

    public IIshtarAllocator Rent<T>(out T* output) where T : unmanaged
    {
        var allocator = 
#if WINDOWS
        new WindowsAllocator();
#elif LINUX
        new NullAllocator();
#elif OSX
        new NullAllocator();
#else
        new NullAllocator();
#endif

        output = (T*)allocator.AllocZeroed(sizeof(T));

        if (allocator is IIshtarAllocatorIdentifier identifier)
            identifier.SetId((nint)output);
        _allocators[allocator.Id] = allocator;

        return allocator;
    }

    public IIshtarAllocator RentArray<T>(out T* output, int size) where T : unmanaged
    {
        var allocator = 
#if WINDOWS
            new WindowsAllocator();
#elif LINUX
        new NullAllocator();
#elif OSX
        new NullAllocator();
#else
        new NullAllocator();
#endif

        output = (T*)allocator.AllocZeroed(sizeof(T) * size);

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
