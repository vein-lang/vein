namespace ishtar.allocators;

public sealed unsafe class NullAllocator : IIshtarAllocator
{
    public long TotalSize { get; }
    public IntPtr Id { get; }

    public void* AllocZeroed(ulong size, AllocationKind kind, CallFrame* frame) => throw new NotImplementedException();

    public void* AllocZeroed(long size, AllocationKind kind, CallFrame* frame) => throw new NotImplementedException();

    public void* AllocZeroed(UIntPtr size, AllocationKind kind, CallFrame* frame) => throw new NotImplementedException();

    public void* AllocZeroed(IntPtr size, AllocationKind kind, CallFrame* frame) => throw new NotImplementedException();

    public void* AllocZeroed(int size, AllocationKind kind, CallFrame* frame) => throw new NotImplementedException();

    public void SetId(IntPtr id) => throw new NotImplementedException();
    public void FreeAll() => throw new NotImplementedException();
}
