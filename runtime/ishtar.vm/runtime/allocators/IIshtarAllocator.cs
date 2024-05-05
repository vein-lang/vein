namespace ishtar.allocators;

public enum AllocationKind
{
    reference,
    no_reference
}

public unsafe interface IIshtarAllocator : IIshtarAllocatorIdentifier, IIshtarAllocatorDisposer
{
    long TotalSize { get; }
    nint Id { get; }

    void* AllocZeroed(ulong size, AllocationKind kind, CallFrame frame);
    void* AllocZeroed(long size, AllocationKind kind, CallFrame frame);
    void* AllocZeroed(nuint size, AllocationKind kind, CallFrame frame);
    void* AllocZeroed(nint size, AllocationKind kind, CallFrame frame);
    void* AllocZeroed(int size, AllocationKind kind, CallFrame frame);
}
