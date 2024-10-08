namespace ishtar.runtime.gc;

public unsafe struct GC_stack_base
{
    public void* mem_base;
}

public unsafe interface GCLayout
{
    public void init();
    public void destroy();
    public void* alloc(uint size);
    public bool isOwnerShip(void** ptr);
    public void free(void** ptr);
    public void create_weak_link(void** child, void* parent, bool trackable);
    public void unlink(void** link_addr, bool trackable);
    public void* alloc_atomic(uint size);
    public void* alloc_immortal(uint size);
    public void* alloc_atomic_immortal(uint size);
    public void add_roots(void* ptr, int size);
    public void remove_roots(void* ptr, int size);
    public void clear_roots();
    public long get_heap_size();
    public long get_free_bytes();
    public GcHeapUsageStat get_heap_usage();
    public bool try_collect();
    public void finalize_all();
    public void finalize_on_demand();
    public bool get_stack_base(GC_stack_base* attr);
    public void register_thread(GC_stack_base* attr);
    public bool is_registered_thread();
    public void unregister_thread();

    public void register_finalizer_no_order(void* obj, delegate*<nint, nint, void> proc, CallFrame* frame);
    public void collect();
    bool is_marked(void* obj);
}
