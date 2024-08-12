namespace ishtar.collections;

public readonly unsafe struct AllocatorBlock(void* parent,
    delegate*<void*, void> free,
    delegate*<void*, uint, void*> realloc,
    delegate*<uint, void*, void*> allocWithHistory,
    delegate*<uint, void*, void*> allocPrimitivesWithHistory)
{
    public readonly delegate*<void*, void> free = free;
    public readonly delegate*<void*, uint, void*> realloc = realloc;


    public readonly delegate*<uint, void*, void*> alloc_with_history = allocWithHistory;
    public readonly delegate*<uint, void*, void*> alloc_primitives_with_history = allocPrimitivesWithHistory;


    public void* alloc(uint size) => alloc_with_history(size, parent);
    public void* alloc_primitives(uint size) => alloc_primitives_with_history(size, parent);
}


public static unsafe class AllocatorBlockEx
{
    public static T* malloc<T>(this AllocatorBlock allocator, uint size) where T : unmanaged
        => (T*)allocator.alloc(size);
    public static T* realloc<T>(this AllocatorBlock allocator, T* data, uint size) where T : unmanaged
        => (T*)allocator.realloc(data, size);
}
