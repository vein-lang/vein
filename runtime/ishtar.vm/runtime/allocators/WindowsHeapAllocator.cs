namespace ishtar.allocators;

using System.Runtime.InteropServices;

public sealed unsafe class WindowsHeapAllocator : IIshtarAllocator
{
    public long TotalSize { get; private set; }
    public nint Id { get; private set; }

    private readonly List<HeapMemRef> Heaps = new();


    internal static class Native
    {
        [DllImport("Kernel32")]
        public static extern nint GetProcessHeap();

        [DllImport("Kernel32")]
        public static extern nint HeapAlloc([In] nint handle, [In] uint dwFlags, [In] nint dwBytes);

        [DllImport("Kernel32")]
        public static extern nint HeapFree([In] nint handle, [In] uint dwFlags, [In] nint target);
    }

    internal record struct HeapMemRef(nint heapHandle, nint memPtr, long size);

    private void* AllocFromHeap(long size, CallFrame frame)
    {
        frame.assert(size != 0, WNE.STATE_CORRUPT, "Allocation is not allowed zero size");

        TotalSize += size;

        var heapHandle = Native.GetProcessHeap();
        
        var mem = Native.HeapAlloc(heapHandle, 0x00000008 | 0x00000004, (nint)size);

        Heaps.Add(new(heapHandle, mem, size));

        return (void*)mem;
    }


    public void* AllocZeroed(ulong size, CallFrame frame)
        => AllocFromHeap((long)size, frame);

    public void* AllocZeroed(long size, CallFrame frame)
        => AllocFromHeap(size, frame);

    public void* AllocZeroed(UIntPtr size, CallFrame frame)
        => AllocFromHeap((long)size, frame);

    public void* AllocZeroed(IntPtr size, CallFrame frame)
        => AllocFromHeap(size, frame);

    public void* AllocZeroed(int size, CallFrame frame)
        => AllocFromHeap(size, frame);

    void IIshtarAllocatorIdentifier.SetId(nint id) => Id = id;

    void IIshtarAllocatorDisposer.FreeAll()
    {
        foreach (var p in Heaps)
        {
            var result = Native.HeapFree(p.heapHandle, 0, p.memPtr);
            var lastError = Marshal.GetLastWin32Error();
            if (result != 0 && lastError != 0)
                throw new InsufficientMemoryException($"Failed dispose heap memory, error: {lastError} {Marshal.GetLastPInvokeErrorMessage()}");
        }
        Heaps.Clear();
    }
}
