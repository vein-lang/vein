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
