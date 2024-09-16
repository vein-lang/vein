namespace ishtar.runtime.gc;

using System;
using static SystemAllocator;


public static unsafe class PlatformAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TEB
    {
        public IntPtr ExceptionList;
        /// <summary>
        /// bottom
        /// </summary>
        public IntPtr StackBase;
        /// <summary>
        /// top
        /// </summary>
        public IntPtr StackLimit;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct THREAD_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr TebBaseAddress;  // Указатель на TEB
        public IntPtr ClientId;
        public IntPtr AffinityMask;
        public IntPtr Priority;
        public IntPtr BasePriority;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern int NtQueryInformationThread(
        IntPtr ThreadHandle,
        int ThreadInformationClass,
        out THREAD_BASIC_INFORMATION ThreadInformation,
        int ThreadInformationLength,
        out int ReturnLength);

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentThread();

    const int ThreadBasicInformation = 0;

    public static TEB GetThreadStack()
    {
        // Для 64-битных систем TEB находится в регистре GS
        int returnLength;
        THREAD_BASIC_INFORMATION tbi;

        IntPtr currentThreadHandle = GetCurrentThread();
        int status = NtQueryInformationThread(currentThreadHandle, ThreadBasicInformation, out tbi, Marshal.SizeOf(typeof(THREAD_BASIC_INFORMATION)), out returnLength);

        if (status == 0) // STATUS_SUCCESS
        {
            IntPtr tebBaseAddress = tbi.TebBaseAddress;

            // Чтение TEB структуры для получения стека
            var teb = Marshal.PtrToStructure<TEB>(tebBaseAddress);

            Console.WriteLine($"StackBottom 0x{teb.StackBase:X}");
            Console.WriteLine($"StackTop 0x{teb.StackLimit:X}");
            return teb;
        }
        else
        {
            Console.WriteLine($"Ошибка запроса информации о потоке, статус: {status}");
        }

        throw new Exception();
    }
}

public unsafe struct ScillaGC
{
    public MemFragMap* allocations;
    public bool paused;
    public size_t min_size;

    MemFrag* alloc_frag(void* ptr, size_t size)
    {
        var frag = malloc<MemFrag>();
        *frag = new MemFrag(ptr, size, ScillaTag.NONE);
        return frag;
    }

    MemFragMap* allocation_map_new(size_t min_capacity,
        size_t capacity, decimal sweep_factor,  decimal downsize_factor, decimal upsize_factor)
    {
        var am = malloc<MemFragMap>();
        am->min_capacity = primes.next_prime(min_capacity);
        am->capacity = primes.next_prime(capacity);
        if (am->capacity < am->min_capacity) am->capacity = am->min_capacity;
        am->sweep_factor = sweep_factor;
        am->sweep_limit = (size_t)(sweep_factor * am->capacity);
        am->downsize_factor = downsize_factor;
        am->upsize_factor = upsize_factor;
        am->allocations = calloc<MemFrag>(am->capacity);
        am->size = 0;
        return am;
    }

    void allocation_map_delete(MemFragMap* am)
    {
        //LOG_DEBUG("Deleting allocation map (cap=%ld, siz=%ld)",
        //    am->capacity, am->size);
        for (size_t i = 0; i < am->capacity; ++i)
        {
            MemFrag* alloc;
            if ((alloc = am->allocations[i]) == null)
                continue;
            while (alloc != null)
            {
                var tmp = alloc;
                alloc = alloc->next;
                delete(tmp);
            }
        }
        free(am->allocations);
        free(am);
    }

    void allocation_map_resize(MemFragMap* am, size_t new_capacity)
    {
        if (new_capacity <= am->min_capacity)
            return;
        // Replaces the existing items array in the hash table
        // with a resized one and pushes items into the new, correct buckets
        //LOG_DEBUG("Resizing allocation map (cap=%ld, siz=%ld) -> (cap=%ld)",
        //    am->capacity, am->size, new_capacity);
        var resized_allocs = calloc<MemFrag>(new_capacity);

        for (size_t i = 0; i < am->capacity; ++i)
        {
            var alloc = am->allocations[i];
            while (alloc != null)
            {
                var next_alloc = alloc->next;
                size_t new_index = hash(alloc->@ref) % new_capacity;
                alloc->next = resized_allocs[new_index];
                resized_allocs[new_index] = alloc;
                alloc = next_alloc;
            }
        }
        free(am->allocations);
        am->capacity = new_capacity;
        am->allocations = resized_allocs;
        am->sweep_limit = (size_t)(am->size + am->sweep_factor * (am->capacity - am->size));
    }

    bool allocation_map_resize_to_fit(MemFragMap* am)
    {
        var load_factor = am->load_factor();
        if (load_factor > am->upsize_factor)
        {
            log("Load factor {0} > {1}, triggering upsize.",
                load_factor, am->upsize_factor);
            allocation_map_resize(am, primes.next_prime(am->capacity * 2));
            return true;
        }
        if (load_factor < am->downsize_factor)
        {
            log("Load factor {0} < {1}, triggering downsize.",
                load_factor, am->downsize_factor);
            allocation_map_resize(am, primes.next_prime(am->capacity / 2));
            return true;
        }
        return false;
    }

    MemFrag* allocation_map_get(MemFragMap* am, void* ptr)
    {
        size_t index = hash(ptr) % am->capacity;
        var cur = am->allocations[index];
        while (cur != null)
        {
            if (cur->@ref == ptr)
                return cur;
            cur = cur->next;
        }
        return null;
    }


    MemFrag* gc_allocation_map_put(MemFragMap* am,
        void* ptr,
        size_t size)
    {
        size_t index = hash(ptr) % am->capacity;
        log("stored request for allocation ix={0}", index);
        var alloc = alloc_frag(ptr, size);
        var cur = am->allocations[index];
        var prev = default(MemFrag*);
        /* Upsert if ptr is already known (e.g. dtor update). */
        while (cur != null)
        {
            if (cur->@ref == ptr)
            {
                // found it
                alloc->next = cur->next;
                if (prev == null)
                    am->allocations[index] = alloc; // position 0
                else
                    prev->next = alloc; // in the list
                delete(cur);
                log("AllocationMap Upsert at ix={0}", index);
                return alloc;

            }
            prev = cur;
            cur = cur->next;
        }
        /* Insert at the front of the separate chaining list */
        cur = am->allocations[index];
        alloc->next = cur;
        am->allocations[index] = alloc;
        am->size++;
        log("AllocationMap insert at ix={0}", index);
        void* p = alloc->@ref;
        if (allocation_map_resize_to_fit(am))
            alloc = allocation_map_get(am, p);
        return alloc;
    }
    void gc_allocation_map_remove(MemFragMap* am,
        void* ptr,
        bool allow_resize)
    {
        // ignores unknown keys
        size_t index = hash(ptr) % am->capacity;
        var cur = am->allocations[index];
        var prev = default(MemFrag*);
        while (cur != null)
        {
            var next = cur->next;
            if (cur->@ref == ptr)
            {
                // found it
                if (prev == null)
                    am->allocations[index] = cur->next; // first item in list
                else
                    prev->next = cur->next; // not the first item in the list
                delete(cur);
                am->size--;
            }
            else
                prev = cur; // move on
            cur = next;
        }
        if (allow_resize) allocation_map_resize_to_fit(am);
    }

    void* mcalloc(size_t count, size_t size)
    {
        if (count == 0) return malloc(size);
        return calloc(count, size);
    }

    public void* allocate(size_t count, size_t size)
    {
        /* Allocation logic that generalizes over malloc/calloc. */

        /* Check if we reached the high-water mark and need to clean up */
        if (gc_needs_sweep() && !paused)
        {
            size_t freed_mem = gc_run();
            log("garbage collection cleaned up {0} bytes.", freed_mem);
        }
        /* With cleanup out of the way, attempt to allocate memory */
        void* ptr = mcalloc(count, size);
        size_t alloc_size = count != 0 ? count * size : size;
        /* If allocation fails, force an out-of-policy run to free some memory and try again. */
        if (ptr == null && !paused /*&& (errno == EAGAIN || errno == ENOMEM)*/)
        {
            gc_run();
            ptr = mcalloc(count, size);
        }
        /* Start managing the memory we received from the system */
        if (ptr != null)
        {
            log("allocated {0} bytes at {1}", alloc_size, (nint)ptr);
            var alloc = gc_allocation_map_put(allocations, ptr, alloc_size);
            /* Deal with metadata allocation failure */
            if (alloc != null)
            {
                log("managing {0} bytes at {1}", alloc_size, (nint)alloc->@ref);
                ptr = alloc->@ref;
            }
            else
            {
                /* We failed to allocate the metadata, fail cleanly. */
                free(ptr);
                ptr = null;
            }
        }
        return ptr;
    }

    public void* realloc(void* p, size_t size)
    {
        var alloc = allocation_map_get(allocations, p);
        if (p != null && alloc == null)
        {
            // the user passed an unknown pointer
            throw new InvalidOperationException();
        }
        void* q = realloc(p, size);
        if (q == null)
        {
            // realloc failed but p is still valid
            return null;
        }
        if (p == null)
        {
            // allocation, not reallocation
            alloc = gc_allocation_map_put(allocations, q, size);
            return alloc->@ref;
        }
        if (p == q)
        {
            // successful reallocation w/o copy
            alloc->size = size;
        }
        else
        {
            // successful reallocation w/ copy
            //void(*dtor)(void *) = alloc->dtor;
            gc_allocation_map_remove(allocations, p, true);
            gc_allocation_map_put(allocations, q, size);
        }
        return q;
    }

    public void delete(void* ptr)
    {
        var alloc = allocation_map_get(allocations, ptr);
        if (alloc != null)
        {
            if (alloc->dtor != null)
            {
                alloc->dtor(ptr);
            }
            free(ptr);
            gc_allocation_map_remove(allocations, ptr, true);
        }
        else
        {
            log("Ignoring request to free unknown pointer {0}", (void*)ptr);
        }
    }


    public void make_root(void* ptr)
    {
        var alloc = allocation_map_get(allocations, ptr);
        if (alloc != null)
        {
            alloc->tag |= ScillaTag.ROOT;
        }
    }

    void* malloc(size_t size) => malloc_ext(size);

    void* malloc_static(size_t size)
    {
        void* ptr = malloc_ext(size);
        make_root(ptr);
        return ptr;
    }


    void* gc_make_static(void* ptr)
    {
        make_root(ptr);
        return ptr;
    }

    void* gc_calloc(size_t count, size_t size)
        => calloc_ext(count, size);

    void* calloc_ext(size_t count, size_t size)
        => allocate(count, size);

    void* malloc_ext(size_t size)
        => allocate(0, size);

    private static void log(string msg, params object[] args) => Console.WriteLine($"[gc-log]: {string.Format(msg, args)}");
    private static void log(string msg, void* ptr, params object[] args) => Console.WriteLine($"[gc-log]: {string.Format(msg, new List<object>(){ (nint)ptr }.Concat(args).ToArray())}");
    private static void log(string msg, void* ptr, void* ptr2) => Console.WriteLine($"[gc-log]: {string.Format(msg, [(nint)ptr, (nint)ptr2])}");


    public void gc_start()
        => gc_start_ext(1024, 1024, 0.2m, 0.8m, 0.5m);

    public void gc_start_ext(
        size_t initial_capacity,
        size_t min_capacity,
        decimal downsize_load_factor,
        decimal upsize_load_factor,
        decimal sweep_factor)
    {
        var downsize_limit = downsize_load_factor > 0.0m ? downsize_load_factor : 0.2m;
        var upsize_limit = upsize_load_factor > 0.0m ? upsize_load_factor : 0.8m;
        sweep_factor = sweep_factor > 0.0m ? sweep_factor : 0.5m;
        paused = false;
        initial_capacity = initial_capacity < min_capacity ? min_capacity : initial_capacity;
        allocations = allocation_map_new(min_capacity, initial_capacity,
            sweep_factor, downsize_limit, upsize_limit);
        log("Created new garbage collector (cap{0}, siz={1}).", allocations->capacity,
            allocations->size);
    }

    void gc_mark_alloc(void* ptr)
    {
        var alloc = allocation_map_get(allocations, ptr);
        /* Mark if alloc exists and is not tagged already, otherwise skip */
        if (alloc == null || (alloc->tag & ScillaTag.MARK) != 0)
            return;
        //log("Marking allocation (ptr={0})", ptr);
        alloc->tag |= ScillaTag.MARK;
        /* Iterate over allocation contents and mark them as well */
        //log("Checking allocation (ptr={0}, size={1}) contents", ptr, alloc->size);
        for (char* p = (char*)alloc->@ref;
             p <= (char*)alloc->@ref + alloc->size - sizeof(size_t);
             ++p)
        {
            //log("Checking allocation (ptr={0}) @{1} with value {2}",
            //    (nint)ptr, (nint)(p - ((char*)alloc->@ref)), (nint)(*(void**)p));
            gc_mark_alloc(*(void**)p);
        }
    }

    void gc_mark_stack()
    {
        log("Marking the stack");
        var b = PlatformAPI.GetThreadStack();
        void* tos = (void*)b.StackLimit;
        void* bos = (void*)b.StackBase;
        /* The stack grows towards smaller memory addresses, hence we scan tos->bos.
         * Stop scanning once the distance between tos & bos is too small to hold a valid pointer */
        for (char* p = (char*)tos; p <= (char*)bos - sizeof(size_t); ++p)
            gc_mark_alloc(*(void**)p);
    }

    void gc_mark_roots()
    {
        for (size_t i = 0; i < allocations->capacity; ++i)
        {
            var chunk = allocations->allocations[i];
            while (chunk != null)
            {
                if ((chunk->tag & ScillaTag.ROOT) != 0)
                {
                    log("Marking root @ {0}", chunk->@ref);
                    gc_mark_alloc(chunk->@ref);
                }
                chunk = chunk->next;
            }
        }
    }

    public void gc_mark()
    {
        /* Note: We only look at the stack and the heap, and ignore BSS. */
        log("Initiating GC mark");
        /* Scan the heap for roots */
        gc_mark_roots();
        /* Dump registers onto stack and scan the stack */
        //void(*volatile _mark_stack)(GarbageCollector*) = gc_mark_stack;
        //jmp_buf ctx;
        //memset(&ctx, 0, sizeof(jmp_buf));
        //setjmp(ctx);
        gc_mark_stack();
    }

    size_t gc_sweep()
    {
        log("Initiating GC sweep");
        size_t total = 0;
        for (size_t i = 0; i < allocations->capacity; ++i)
        {
            var chunk = allocations->allocations[i];
            MemFrag* next = null;
            /* Iterate over separate chaining */
            while (chunk != null)
            {
                if ((chunk->tag & ScillaTag.MARK) != 0)
                {
                    log("Found used allocation {0} (ptr={1})", (void*)chunk, (void*)chunk->@ref);
                    /* unmark */
                    chunk->tag &= ~ScillaTag.MARK;
                    chunk = chunk->next;
                }
                else
                {
                    log("Found unused allocation {0} ({1} bytes @ ptr={2})", (nint)chunk, chunk->size, (nint)chunk->@ref);
                    /* no reference to this chunk, hence delete it */
                    total += chunk->size;
                    if (chunk->dtor != null)
                    {
                        chunk->dtor(chunk->@ref);
                    }
                    free(chunk->@ref);
                    /* and remove it from the bookkeeping */
                    next = chunk->next;
                    gc_allocation_map_remove(allocations, chunk->@ref, false);
                    chunk = next;
                }
            }
        }
        allocation_map_resize_to_fit(allocations);
        return total;
    }

    void gc_unroot_roots()
    {
        log("Unmarking roots");
        for (size_t i = 0; i < allocations->capacity; ++i)
        {
            var chunk = allocations->allocations[i];
            while (chunk != null)
            {
                if ((chunk->tag & ScillaTag.ROOT) != 0)
                    chunk->tag &= ~ScillaTag.ROOT;
                chunk = chunk->next;
            }
        }
    }

    public size_t gc_stop()
    {
        gc_unroot_roots();
        size_t collected = gc_sweep();
        allocation_map_delete(allocations);
        return collected;
    }

    public size_t gc_run()
    {
        log("Initiating GC run");
        gc_mark();
        return gc_sweep();
    }

    public void gc_pause() => paused = true;

    public void gc_resume() => paused = false;

    bool gc_needs_sweep() => allocations->size > allocations->sweep_limit;

    static size_t hash(void* ptr) => ((size_t)ptr) >> 3;

    static void delete(MemFrag* frag) => free(frag);
}





[Flags]
public enum ScillaTag : byte
{
    NONE,
    ROOT,
    MARK
}

public unsafe struct MemFrag(void* @ref, size_t size, ScillaTag tag)
{
    public readonly void* @ref = @ref;
    public size_t size = size;
    public delegate*<void*, void> dtor;
    public ScillaTag tag = tag;
    public MemFrag* next;
}

public unsafe struct MemFragMap
{
    public size_t capacity;
    public size_t min_capacity;
    public decimal downsize_factor;
    public decimal upsize_factor;
    public decimal sweep_factor;
    public size_t sweep_limit;
    public size_t size;
    public MemFrag** allocations;

    public decimal load_factor()
        => size / capacity;
}



public unsafe struct SystemAllocator
{
    public static void* malloc(size_t size)
        => NativeMemory.Alloc(size);
    public static T* malloc<T>() where T : unmanaged
        => (T*)NativeMemory.Alloc((size_t)sizeof(T));
    public static T** calloc<T>(size_t size) where T : unmanaged
        => (T**)NativeMemory.AllocZeroed((size_t)sizeof(T) * size);
    public static void** calloc(size_t size, size_t count)
        => (void**)NativeMemory.AllocZeroed(count * size);
    public static void free(void* ptr)
        => NativeMemory.Free(ptr);
    public static void free<T>(T* ptr) where T : unmanaged
        => NativeMemory.Free(ptr);
    public static void free<T>(T** ptr) where T : unmanaged
        => NativeMemory.Free(ptr);
}
