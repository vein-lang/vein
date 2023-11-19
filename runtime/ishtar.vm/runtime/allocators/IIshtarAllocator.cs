namespace ishtar.allocators;

public unsafe interface IIshtarAllocator : IIshtarAllocatorIdentifier, IIshtarAllocatorDisposer
{
    long TotalSize { get; }
    nint Id { get; }

    void* AllocZeroed(ulong size, CallFrame frame);
    void* AllocZeroed(long size, CallFrame frame);
    void* AllocZeroed(nuint size, CallFrame frame);
    void* AllocZeroed(nint size, CallFrame frame);
    void* AllocZeroed(int size, CallFrame frame);
}