namespace ishtar.allocators;


public unsafe interface IIshtarAllocatorPool
{
    IIshtarAllocator Rent<T>(out T* output, AllocationKind kind, CallFrame* frame) where T : unmanaged;
    IIshtarAllocator RentArray<T>(out T* output, int size, CallFrame* frame, AllocationKind kind = AllocationKind.reference) where T : unmanaged;
    long Return(nint p);
    long Return(void* p);
}
