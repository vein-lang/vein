namespace ishtar.runtime.gc;

using System.Runtime.InteropServices;

public unsafe class BoehmGCLayout : GCLayout, GCLayout_Debug
{
    public class GcNotLoaded : Exception;
    public class GcAlreadyLoaded : Exception;
    public class PointerIsNotGcOwnership : Exception;

    public class Native
    {
        public static void Load() =>
            NativeLibrary.Load(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "includes/gc.dll"
                : "includes/libgc.so");

        public const string LIBNAME = "gc";


        [DllImport(LIBNAME)]
        public static extern void GC_init();
        // GC_API void GC_CALL GC_deinit(void);
        [DllImport(LIBNAME)]
        public static extern void GC_deinit();
        [DllImport(LIBNAME)]
        public static extern bool GC_is_init_called();


        // && for safe destroy

        [DllImport(LIBNAME)]
        public static extern void GC_clear_exclusion_table();
        [DllImport(LIBNAME)]
        public static extern void GC_clear_roots();

        public static void SafeDestroy()
        {
            GC_clear_exclusion_table();
            GC_clear_roots();
            GC_deinit();
        }
        

        [DllImport(LIBNAME)]
        public static extern void GC_free(void* ptr);

        [DllImport(LIBNAME)]
        public static extern bool GC_is_heap_ptr(void* ptr);
        [DllImport(LIBNAME)]
        public static extern void* GC_malloc(uint size);

        [DllImport(LIBNAME)]
        public static extern void GC_register_finalizer_for_ptr(void* ptr, delegate*<void> finalizer);
        [DllImport(LIBNAME)]
        public static extern void* GC_malloc_atomic(uint size);
        [DllImport(LIBNAME)]
        public static extern void GC_collect();
        [DllImport(LIBNAME)]
        public static extern void GC_dump(void* file);
        [DllImport(LIBNAME)]
        public static extern int GC_try_to_collect(int zero);
        [DllImport(LIBNAME)]
        public static extern void GC_general_register_disappearing_link(void** location, void* obj);

        // GC_API int GC_CALL GC_unregister_long_link(void * * link)
        [DllImport(LIBNAME)]
        public static extern void GC_unregister_long_link(void** location);

        // GC_API int GC_CALL GC_unregister_disappearing_link(void * * link)
        [DllImport(LIBNAME)]
        public static extern void GC_unregister_disappearing_link(void** location);

        /* Another versions of the above follow.  It ignores            */
        /* self-cycles, i.e. pointers from a finalizable object to      */
        /* itself.  There is a stylistic argument that this is wrong,   */
        /* but it's unavoidable for C++, since the compiler may         */
        /* silently introduce these.  It's also benign in that specific */
        /* case.  And it helps if finalizable objects are split to      */
        /* avoid cycles.                                                */
        /* Note that cd will still be viewed as accessible, even if it  */
        /* refers to the object itself.                                 */
        // GC_API void GC_CALL GC_register_finalizer_ignore_self(void* /* obj */, GC_finalization_proc /* fn */, void* /* cd */, GC_finalization_proc* /* ofn */, void** /* ocd */) GC_ATTR_NONNULL(1);
        [DllImport(LIBNAME)]
        public static extern void GC_register_finalizer_ignore_self(void* obj, delegate*<nint, nint, void> fn, void* cd, delegate*<nint, nint, void> ofn, void** ocd);


        /* Register a given object for toggle-ref processing.  It will  */
        /* be stored internally and the toggle-ref callback will be     */
        /* invoked on the object until the callback returns             */
        /* GC_TOGGLE_REF_DROP or the object is collected.  If is_strong */
        /* is true, then the object is registered with a strong ref,    */
        /* a weak one otherwise.  Obj should be the starting address    */
        /* of an object allocated by GC_malloc (GC_debug_malloc) or     */
        /* friends.  Returns GC_SUCCESS if registration succeeded (or   */
        /* no callback is registered yet), GC_NO_MEMORY if it failed    */
        /* for a lack of memory reason.                                 */
        //GC_API int GC_CALL GC_toggleref_add(void* /* obj */, int /* is_strong */)
        //GC_ATTR_NONNULL(1);
        //GC_API int GC_CALL GC_debug_toggleref_add(void* /* obj */,
        //    int /* is_strong */) GC_ATTR_NONNULL(1);

        [DllImport(LIBNAME)]
        public static extern int GC_toggleref_add(void* obj, int is_stronks);


        [DllImport(LIBNAME, CharSet = CharSet.Ansi)]
        public static extern void GC_register_finalizer_no_order(void* obj, delegate*<nint, nint, void> fn, void* cd, delegate*<nint, nint, void> ofn, void** ocd);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GC_finalization_proc(void* p, void* i);

        // GC_API int GC_CALL GC_register_long_link(void** /* link */, const void * /* obj */) GC_ATTR_NONNULL(1) GC_ATTR_NONNULL(2);
        [DllImport(LIBNAME)]
        public static extern void GC_register_long_link(void** link, void* obj);


        /* Given a pointer to the base of an object, return its size in bytes.  */
        /* The returned size may be slightly larger than what was originally    */
        /* requested.                                                           */
        //GC_API size_t GC_CALL GC_size(const void * /* obj_addr */) GC_ATTR_NONNULL(1);
        [DllImport(LIBNAME)]
        public static extern uint GC_size(void* ptr);

        [DllImport(LIBNAME)]
        public static extern void GC_gcollect();

        /* Set and get the default stop_func.  The default stop_func is used by */
        /* GC_gcollect() and by implicitly triggered collections (except for    */
        /* the case when handling out of memory).  Must not be 0.  Both the     */
        /* setter and the getter acquire the allocator lock (in the reader mode */
        /* in case of the getter) to avoid data race.                           */
        // GC_API void GC_CALL GC_set_stop_func(GC_stop_func /* stop_func */) GC_ATTR_NONNULL(1);
        [DllImport(LIBNAME)]
        public static extern uint GC_set_stop_func(GC_stop_func ptr);

        // GC_API GC_ATTR_MALLOC void * GC_CALL GC_debug_malloc_atomic_uncollectable(size_t lb, GC_EXTRA_PARAMS)
        [DllImport(LIBNAME)]
        public static extern nint GC_malloc_atomic_uncollectable(uint size);

        [DllImport(LIBNAME)]
        public static extern nint GC_malloc_uncollectable(uint size);

        [DllImport(LIBNAME)]
        public static extern nint GC_realloc(nint oldPtr, uint newSize);

        // GC_finalize_all

        [DllImport(LIBNAME)]
        public static extern void GC_finalize_all();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GC_stop_func();


        // GC_API void GC_CALL GC_set_find_leak(int);
        [DllImport(LIBNAME)]
        public static extern void GC_set_find_leak(bool zero);

        [DllImport(LIBNAME)]
        public static extern long GC_get_heap_size();

        [DllImport(LIBNAME)]
        public static extern long GC_get_free_bytes();

        [DllImport(LIBNAME)]
        public static extern void GC_get_heap_usage_safe(long* pheap_size, long* pfree_bytes, long* punmapped_bytes, long* pbytes_since_gc, long* ptotal_bytes);

        [DllImport(LIBNAME)]
        public static extern bool GC_is_marked(void* ptr);

        [DllImport(LIBNAME)]
        public static extern bool GC_add_roots(void* hi, void* low);
        
        [DllImport(LIBNAME)]
        public static extern void GC_remove_roots(void* hi, void* low);


        [DllImport(LIBNAME)]
        public static extern nint GC_is_visible(void* ptr);




        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GC_is_visible_print_proc_func(void* ptr);

        [DllImport(LIBNAME)]
        public static extern nint GC_is_visible_print_proc(GC_is_visible_print_proc_func proc);


        /* Allocate an object of size lb bytes.  The client guarantees that as  */
        /* long as the object is live, it will be referenced by a pointer that  */
        /* points to somewhere within the first GC heap block (hblk) of the     */
        /* object.  (This should normally be declared volatile to prevent the   */
        /* compiler from invalidating this assertion.)  This routine is only    */
        /* useful if a large array is being allocated.  It reduces the chance   */
        /* of accidentally retaining such an array as a result of scanning an   */
        /* integer that happens to be an address inside the array.  (Actually,  */
        /* it reduces the chance of the allocator not finding space for such    */
        /* an array, since it will try hard to avoid introducing such a false   */
        /* reference.)  On a SunOS 4.X or Windows system this is recommended    */
        /* for arrays likely to be larger than 100 KB or so.  For other systems,*/
        /* or if the collector is not configured to recognize all interior      */
        /* pointers, the threshold is normally much higher.                     */
        [DllImport(LIBNAME)]
        public static extern void* GC_malloc_ignore_off_page(uint size);
    }


    public void init()
    {
        Native.GC_init();
        Native.GC_set_find_leak(true);
    }

    public void destroy() => Native.GC_deinit();

    public void* alloc(uint size)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        return Native.GC_malloc(size);
    }

    /* Explicitly deallocate an object.  Dangerous if used incorrectly.     */
    /* Requires a pointer to the base of an object.                         */
    /* An object should not be enabled for finalization (and it should not  */
    /* contain registered disappearing links of any kind) when it is        */
    /* explicitly deallocated.                                              */
    /* GC_free(0) is a no-op, as required by ANSI C for free.               */
    public void free(void** ptr)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        if (Native.GC_is_heap_ptr(*ptr))
        {
            Native.GC_free(*ptr);
            *ptr = null;
        }
        else
            throw new PointerIsNotGcOwnership();
    }

    public bool isOwnerShip(void** ptr)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        return Native.GC_is_heap_ptr(*ptr);
    }

    private static nuint hide_pointer(void* p) => ~(nuint)p;

    public void create_weak_link(void** link_addr, void* obj, bool trackable)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        *link_addr = (void*)hide_pointer(obj);
        if (trackable)
            Native.GC_register_long_link(link_addr, obj);
        else
            Native.GC_general_register_disappearing_link(link_addr, obj);
    }

    public void unlink(void** link_addr, bool trackable)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        if (trackable)
            Native.GC_unregister_long_link(link_addr);
        else
            Native.GC_unregister_disappearing_link(link_addr);
        *link_addr = null;
    }

    public void create_weak_link(void** child, void* parent)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        Native.GC_general_register_disappearing_link(child, parent);
    }

    public void register_finalizer_ignore_self(void* obj, delegate*<nint, nint, void> proc)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        Native.GC_register_finalizer_ignore_self(obj, proc, null, null, null);
    }

    public void* alloc_atomic(uint size)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        return (void*)Native.GC_malloc_atomic_uncollectable(size);
    }


    private struct WeakImmortalRef(void** addr, void* obj)
    {

    }

    private static readonly List<WeakImmortalRef> _weak_refs = new List<WeakImmortalRef>();

    public void* alloc_immortal(uint size)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        //if (root_block == 0)
        //{
        //    Interlocked.MemoryBarrier();
        //    var data = (nint**)Native.GC_malloc_ignore_off_page(totalSize);

        //    Interlocked.Exchange(ref root_block, (nint)data);

        //    Native.GC_add_roots((nint**)root_block, (nint**)(root_block + (totalSize)));
        //}

        //if (_offset + size > totalSize)
        //{
        //    totalSize *= 2;
        //    Interlocked.Exchange(ref root_block, Native.GC_debug_realloc(root_block, totalSize, "vein.v", 0));
        //    throw new InsufficientMemoryException();
        //}


        var ptr = (void*)Native.GC_malloc_atomic_uncollectable(size);


        var result = Native.GC_toggleref_add(ptr, 0);


        if (result == 0)
        {

        }

        //var weakData = (void**)NativeMemory.AllocZeroed((uint)sizeof(nint), 2);

        //_weak_refs.Add(new WeakImmortalRef(weakData, ptr));

        //*weakData = (void*)(~(int)1 & (int)ptr);

        //Native.GC_register_long_link(weakData, ptr);

        //create_weak_link(weakData, ptr, false);


        //((nint**)root_block)[_offset] = ptr;
        //_offset+=sizeof(nint);

        return ptr;
    }

    public void add_roots(void* ptr, int size)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        Native.GC_add_roots(ptr, (void*)((nint)ptr + size));
    }

    public void remove_roots(void* ptr, int size)
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        Native.GC_remove_roots(ptr, (void*)((nint)ptr + size));
    }

    public void clear_roots() => Native.GC_clear_roots();

    public long get_free_bytes() => Native.GC_get_free_bytes();

    public long get_heap_size() => Native.GC_get_heap_size();

    public GcHeapUsageStat get_heap_usage()
    {
        GcHeapUsageStat p1;
        Native.GC_get_heap_usage_safe(&p1.pheap_size, &p1.pfree_bytes, &p1.punmapped_bytes, &p1.pbytes_since_gc, &p1.ptotal_bytes);

        return p1;
    }


    public bool try_collect()
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        return Native.GC_try_to_collect(0) == 1;
    }

    public void collect()
    {
        if (!Native.GC_is_init_called())
            throw new GcNotLoaded();
        Native.GC_gcollect();
    }

    public bool is_marked(void* obj) => Native.GC_is_marked(obj);

    public void finalize_all() => Native.GC_finalize_all();

    public void finalize_on_demand() => throw new NotImplementedException();

    public void find_leak() => throw new NotImplementedException();


    public void register_finalizer_no_order(IshtarObject* obj, delegate*<nint, nint, void> proc, CallFrame frame)
        => Native.GC_register_finalizer_no_order(obj, proc, null, null, null);


    public static void register_finalizer_no_order2(void* obj, delegate*<nint, nint, void> proc)
        => Native.GC_register_finalizer_no_order(obj, proc, null, null, null);


    public void dump(string file)
    {
        //var ptr = Marshal.StringToHGlobalAnsi(file);
        //Native.GC_dump((void*)ptr);
        //Marshal.ZeroFreeGlobalAllocAnsi(ptr);
    }
}
