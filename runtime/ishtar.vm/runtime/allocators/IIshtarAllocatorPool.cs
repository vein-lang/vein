namespace ishtar.allocators;

using System.Runtime.CompilerServices;
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
    long TotalSize { get; }
    nint Id { get; }

    void* AllocZeroed(ulong size);
    void* AllocZeroed(long size);
    void* AllocZeroed(nuint size);
    void* AllocZeroed(nint size);
    void* AllocZeroed(int size);
}

public sealed unsafe class DebugManagedAllocator : IIshtarAllocator
{
    private List<ManagedMemHandle> handles = new List<ManagedMemHandle>();

    public long TotalSize { get; private set; }
    public nint Id { get; private set; }



    private void* Alloc(nint size)
    {
        var handle = System.GC.AllocateArray<byte>((int)size, true);

        fixed (byte* p = handle)
        {
            handles.Add(new(size, handle, (nint)p));

            return p;
        }
    }


    internal record struct ManagedMemHandle(nint size, byte[] handle, nint originalAddr);

    public void* AllocZeroed(ulong size)
    {
        TotalSize += (long)size;
        return Alloc((nint)size);
    }

    public void* AllocZeroed(long size)
    {
        TotalSize += (long)size;
        return Alloc((nint)size);
    }

    public void* AllocZeroed(UIntPtr size)
    {
        TotalSize += (long)size;
        return Alloc((nint)size);
    }

    public void* AllocZeroed(IntPtr size)
    {
        TotalSize += (long)size;
        return Alloc((nint)size);
    }

    public void* AllocZeroed(int size)
    {
        TotalSize += (long)size;
        return Alloc((nint)size);
    }

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;

    public static void AssertStackStillSizeEqual(stackval* pointer, int size = 64)
    {
        //var p = (nint)(pointer);
        //var handler = GCHandle.FromIntPtr(p);
        //var bytes = (byte[])handler.Target;


        //var original = IshtarAllocatorPool.pool._allocators[(nint)pointer];

        //var o = (original as DebugManagedAllocator).handles;

        //var b = o[0];


        //Console.WriteLine("");
    }

    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in handles)
        {
            //if (!p.handle.IsAllocated)
            //    throw new AccessViolationException("Trying free already disposed memory");
            //var targetBytes = (byte[])p.handle.Target;

            //var otherHandler = GCHandle.FromIntPtr(p.originalAddr);
            //var otherBytes = (byte[])otherHandler.Target;
            Console.WriteLine(p.size);
            byte* a = (byte*)p.originalAddr;
            
            //if (targetBytes.Length != p.size)
            //    throw new AccessViolationException("Unequal size of memory trying dispose");

            //p.handle.Free();
        }
        handles.Clear();
    }
}


public sealed unsafe class WindowsAllocator : IIshtarAllocator
{
    public long TotalSize { get; private set; }
    public nint Id { get; private set; }

    private List<HeapMemRef> Heaps = new List<HeapMemRef>();


    internal static class Native
    {
        [DllImport("Kernel32")]
        public static extern nint GetProcessHeap();

        [DllImport("Kernel32")]
        public static extern nint HeapAlloc([In] nint handle, [In] uint dwFlags, [In] nint dwBytes);

        [DllImport("Kernel32")]
        public static extern nint HeapFree([In] nint handle, [In] uint dwFlags, [In] nint target);
    }

    internal record struct HeapMemRef(nint heapHandle, nint memPtr, nint size);

    private nint AllocFromHeap(nint size)
    {
        var heapHandle = Native.GetProcessHeap();
        
        var mem = Native.HeapAlloc(heapHandle, 0x00000008 | 0x00000004 | 0x00000001, size);

        Heaps.Add(new(heapHandle, mem, size));

        return mem;
    }


    public void* AllocZeroed(ulong size)
    {
        TotalSize += (long)size;
        return (void*)AllocFromHeap((nint)size);
    }

    public void* AllocZeroed(long size)
    {
        TotalSize += (long)size;
        return (void*)AllocFromHeap((nint)size);
    }

    public void* AllocZeroed(UIntPtr size)
    {
        TotalSize += (long)size;
        return (void*)AllocFromHeap((nint)size);
    }

    public void* AllocZeroed(IntPtr size)
    {
        TotalSize += (long)size;
        return (void*)AllocFromHeap((nint)size);
    }

    public void* AllocZeroed(int size)
    {
        TotalSize += (long)size;
        return (void*)AllocFromHeap((nint)size);
    }

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;

    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in Heaps)
        {
            var result = Native.HeapFree(p.heapHandle, 0x00000001, p.memPtr);
            var lastError = Marshal.GetLastWin32Error();
            if (result != 0 && lastError != 0)
                throw new InsufficientMemoryException($"Failed dispose heap memory, error: {lastError} {Marshal.GetLastPInvokeErrorMessage()}");
        }
        Heaps.Clear();
    }
}

public sealed unsafe class NativeMemory_WindowsAllocator : IIshtarAllocator
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
        foreach (var p in Memory)
            NativeMemory.Free((void*)p);
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
    internal readonly Dictionary<nint, IIshtarAllocator> _allocators = new();
    
    public IIshtarAllocator Rent<T>(out T* output) where T : unmanaged
    {
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
            new NativeMemory_WindowsAllocator();
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
