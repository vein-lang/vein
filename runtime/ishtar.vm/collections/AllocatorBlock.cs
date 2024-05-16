namespace ishtar.collections;

public unsafe struct AllocatorBlock
{
    public delegate*<uint, void*> alloc;
    public delegate*<uint, void*> alloc_primitives;
    public delegate*<void*, void> free;
    public delegate*<void*, uint, void*> realloc;
}
