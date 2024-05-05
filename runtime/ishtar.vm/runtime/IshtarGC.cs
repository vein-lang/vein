namespace ishtar.vm.runtime
{
    using allocators;
    using ishtar;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using vein.runtime;
    using static vein.runtime.VeinTypeCode;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void IshtarFinalizationProc(nint p, nint i);



    public unsafe interface GCLayout
    {
        public void init();
        public void destroy();
        public void* alloc(uint size);
        public void free(void* ptr);
        public void link(void** child, void* parent, bool trackable);
        public void unlink(void** link_addr, bool trackable);
        public void* alloc_atomic(uint size);
        public void* alloc_immortal(uint size);
        public void add_roots(void* ptr);
        public long get_heap_size();
        public long get_free_bytes();
        public long get_heap_usage();
        public bool try_collect();
        public void finalize_all();
        public void finalize_on_demand();

        public void register_finalizer_no_order(IshtarObject* obj, IshtarFinalizationProc proc, CallFrame frame);
        public void collect();
        bool is_marked(void* obj);
    }

    public unsafe interface GCLayout_Debug
    {
        public void find_leak();
        public void dump(string file);
    }


    public unsafe class BoehmGCLayout : GCLayout, GCLayout_Debug
    {
        public class Native
        {
#if WINDOWS
            public const string LIBNAME = "gc.dll";
#elif LINUX
            public const string LIBNAME = "libgc";
#elif MACOS
            public const string LIBNAME = "libgc";
#else
#error No OS Selected, Boehm not support this os or not implemented 
#endif


            [DllImport(LIBNAME)]
            public static extern void GC_init();
            // GC_API void GC_CALL GC_deinit(void);
            [DllImport(LIBNAME)]
            public static extern void GC_deinit();

            [DllImport(LIBNAME, CharSet = CharSet.Ansi)]
            public static extern void GC_debug_free(void* ptr, string file, int line);
            [DllImport(LIBNAME, CharSet = CharSet.Ansi)]
            public static extern void* GC_debug_malloc(uint size, string file, int line);

            [DllImport(LIBNAME)]
            public static extern void GC_register_finalizer_for_ptr(void* ptr, delegate*<void> finalizer);
            [DllImport(LIBNAME, CharSet = CharSet.Ansi)]
            public static extern void* GC_debug_malloc_atomic(uint size, string file, int line);
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
            public static extern void GC_register_finalizer_ignore_self(void* obj, IshtarFinalizationProc fn, void* cd, IshtarFinalizationProc ofn, void** ocd);



            [DllImport(LIBNAME, CharSet = CharSet.Ansi)]
            public static extern void GC_debug_register_finalizer_no_order(void* obj, IshtarFinalizationProc fn, void* cd, IshtarFinalizationProc ofn, void** ocd, string file, int line);


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
            public static extern uint GC_collect(void* ptr);

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


            // GC_finalize_all

            [DllImport(LIBNAME)]
            public static extern void GC_finalize_all();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void GC_stop_func();


            // GC_API void GC_CALL GC_set_find_leak(int);
            [DllImport(LIBNAME)]
            public static extern void GC_set_find_leak(bool zero);

            [DllImport(LIBNAME)]
            public static extern long get_heap_size();

            [DllImport(LIBNAME)]
            public static extern long get_free_bytes();

            [DllImport(LIBNAME)]
            public static extern long GC_get_heap_usage();

            [DllImport(LIBNAME)]
            public static extern bool GC_is_marked(void* ptr);
        }


        public void init()
        {
            Native.GC_init();
            Native.GC_set_find_leak(true);
        }

        public void destroy() => Native.GC_deinit();

        public void* alloc(uint size) => Native.GC_debug_malloc(size, "empty.vein", 0);

        /* Explicitly deallocate an object.  Dangerous if used incorrectly.     */
        /* Requires a pointer to the base of an object.                         */
        /* An object should not be enabled for finalization (and it should not  */
        /* contain registered disappearing links of any kind) when it is        */
        /* explicitly deallocated.                                              */
        /* GC_free(0) is a no-op, as required by ANSI C for free.               */
        public void free(void* ptr) => Native.GC_debug_free(ptr, "empty.vein", 0);

        private static nuint hide_pointer(void* p) => ~(nuint)p;

        public void link(void** link_addr, void* obj, bool trackable)
        {
            *link_addr = (void*)hide_pointer(obj);
            if (trackable)
                Native.GC_register_long_link(link_addr, obj);
            else
                Native.GC_general_register_disappearing_link(link_addr, obj);
        }

        public void unlink(void** link_addr, bool trackable)
        {
            if (trackable)
                Native.GC_unregister_long_link(link_addr);
            else
                Native.GC_unregister_disappearing_link(link_addr);
            *link_addr = null;
        }

        public void link(void** child, void* parent) => Native.GC_general_register_disappearing_link(child, parent);
        public void register_finalizer_ignore_self(void* obj, IshtarFinalizationProc proc) => Native.GC_register_finalizer_ignore_self(obj, proc, null, null, null);

        public void* alloc_atomic(uint size) => throw new NotImplementedException();

        public void* alloc_immortal(uint size) => throw new NotImplementedException();

        public void add_roots(void* ptr) => throw new NotImplementedException();

        public long get_free_bytes() => Native.get_free_bytes();

        public long get_heap_size() => Native.get_heap_size();

        public long get_heap_usage() => Native.GC_get_heap_usage();

        public bool try_collect() => Native.GC_try_to_collect(0) == 1;

        public void collect() => Native.GC_gcollect();
        public bool is_marked(void* obj) => Native.GC_is_marked(obj);

        public void finalize_all() => Native.GC_finalize_all();

        public void finalize_on_demand() => throw new NotImplementedException();

        public void find_leak() => throw new NotImplementedException();


        public void register_finalizer_no_order(IshtarObject* obj, IshtarFinalizationProc proc, CallFrame frame)
            => Native.GC_debug_register_finalizer_no_order(obj, proc, null, null, null, frame.Debug_GetFile(), frame.Debug_GetLine());


        public void dump(string file)
        {
            //var ptr = Marshal.StringToHGlobalAnsi(file);
            //Native.GC_dump((void*)ptr);
            //Marshal.ZeroFreeGlobalAllocAnsi(ptr);
        }
    }


    public unsafe class IshtarGC(VirtualMachine vm) : IDisposable
    {
        public readonly GCStats Stats = new();
        private readonly LinkedList<nint> RefsHeap = new();
        private readonly LinkedList<nint> ArrayRefsHeap = new();
        private readonly LinkedList<nint> ImmortalHeap = new();
        private readonly LinkedList<nint> TemporaryHeap = new();
        private readonly LinkedList<ConstantTypeMemory> ConstantTypeHeap = new();
        private readonly Dictionary<nint, IshtarObjectMetadata> AllocationMetadata = new();

        private readonly LinkedList<AllocationDebugInfo> allocationTreeDebugInfos = new();
        private readonly Dictionary<nint, AllocationDebugInfo> allocationDebugInfos = new();

        private IIshtarAllocatorPool allocatorPool;

#if BOEHM_GC
        private GCLayout gcLayout = new BoehmGCLayout();
#else
        private GCLayout gcLayout = null;
#error No defined GC layout
#endif

        public string DebugGet() =>
            $"RefsHeap: {RefsHeap.Count}\n" +
            $"ArrayRefsHeap: {ArrayRefsHeap.Count}\n" +
            $"ImmortalHeap: {ImmortalHeap.Count}\n" +
            $"TemporaryHeap: {TemporaryHeap.Count}\n" +
            $"ConstantTypeHeap: {ConstantTypeHeap.Count}\n" +
            $"AllocationMetadata: {AllocationMetadata.Count}\n";

        public class GCStats
        {
            private ulong TotalAllocation;
            private ulong TotalBytesAllocated;

            public long total_allocations
            {
                get => (long)TotalAllocation;
                set => TotalAllocation = checked((ulong)value);
            }

            public long total_bytes_requested
            {
                set => TotalBytesAllocated = checked((ulong)value);
                get => (long)TotalBytesAllocated;
            }
            public ulong alive_objects;
        }

        public record AllocationDebugInfo(ulong BytesAllocated, string Method, nint pointer)
        {
            public string Trace;

            public void Bump() => Trace = new StackTrace(true).ToString();
        }

        public class ConstantTypeMemory(RuntimeIshtarClass @class) : IDisposable
        {
            public RuntimeIshtarClass Class = @class;
            public LinkedList<IDisposable> RefsPool = new LinkedList<IDisposable>();

            public void Dispose()
            {
                Class.Dispose();
                foreach (var disposable in RefsPool)
                    disposable.Dispose();
            }

            public static ConstantTypeMemory Create(RuntimeIshtarClass @class, IshtarGC gc)
            {
                var p = new ConstantTypeMemory(@class);

                gc.ConstantTypeHeap.AddLast(p);

                return p;
            }
        }


        public enum GcColor
        {
            RED,
            GREEN,
            YELLOW
        }

        public record IshtarObjectMetadata(nint obj_ref)
        {
            public ulong ReferenceCount => (ulong)_references.Count;
            private readonly LinkedList<IshtarObjectMetadata> _references = new();

            // TODO, if ref count is zero, 1 / 0 just open black hole
            public double Priority => 1d / ReferenceCount;
            public GcColor Color { get; private set; } = GcColor.RED;

            public IshtarObject* Target => (IshtarObject*)obj_ref;



            public void AddRef(IshtarObjectMetadata metadata)
            {
                _references.AddLast(metadata);
                if (Color == GcColor.GREEN)
                    Color = GcColor.YELLOW;
            }

            public void RemoveRef(IshtarObjectMetadata metadata)
            {
                _references.Remove(metadata);
                if (Color == GcColor.GREEN)
                    Color = GcColor.YELLOW;
            }
        }

        public void init()
        {
            if (root is not null)
                return;
            allocatorPool = new IshtarAllocatorPool(gcLayout);
            root = AllocObject(TYPE_OBJECT.AsRuntimeClass(VM.Types), vm.Frames.GarbageCollector());
        }

        public IshtarObject* root;
        public VirtualMachine VM => vm;
        public bool check_memory_leak = true;

        public void Dispose()
        {
            foreach (var memory in ConstantTypeHeap)
                memory.Dispose();
            ConstantTypeHeap.Clear();

            foreach (var p in RefsHeap.ToArray())
            {
                FreeObject((IshtarObject*)p, VM.Frames.GarbageCollector());
            }
            RefsHeap.Clear();

            foreach (var p in ArrayRefsHeap.ToArray())
            {
                FreeArray((IshtarArray*)p, VM.Frames.GarbageCollector());
            }
            ArrayRefsHeap.Clear();


            //foreach (var p in TemporaryHeap)
            //{
            //    NativeMemory.Free((void*)p);
            //    Stats.total_allocations--;
            //    Stats.total_bytes_requested -= (ulong)sizeof(stackval);
            //}

            if (!check_memory_leak) return;

            if (Stats.total_allocations != 0)
            {
                vm.FastFail(WNE.MEMORY_LEAK, $"After clear all allocated memory, total_allocations is not zero ({Stats.total_allocations})", VM.Frames.GarbageCollector());
                return;
            }
            if (Stats.total_bytes_requested != 0)
            {
                vm.FastFail(WNE.MEMORY_LEAK, $"After clear all allocated memory, total_bytes_requested is not zero ({Stats.total_bytes_requested})", VM.Frames.GarbageCollector());
                return;
            }
        }

        public ImmortalObject<T>* AllocStatic<T>() where T : class, new()
        {
            throw null;
            //var allocator = allocatorPool.Rent<ImmortalObject<T>>(out var p);

            //p->Create(new T());

            //ImmortalHeap.AddLast((nint)p);

            //InsertDebugData(new((ulong)sizeof(ImmortalObject<T>),
            //    nameof(AllocImmortal), (nint)p));

            //Stats.total_allocations++;
            //Stats.total_bytes_requested += allocatorPool.Return(allocator);

            //return p;
        }


        private void InsertDebugData(AllocationDebugInfo info)
        {
            allocationTreeDebugInfos.AddLast(info);
            allocationDebugInfos[info.pointer] = info;
            info.Bump();
        }

        private void DeleteDebugData(nint pointer)
            => allocationTreeDebugInfos.Remove(allocationDebugInfos[pointer]);

        public void FreeStatic<T>(ImmortalObject<T>* p) where T : class, new()
        {
            throw null;
            //DeleteDebugData((nint)p);

            //Stats.total_allocations--;
            //Stats.total_bytes_requested -= (ulong)sizeof(ImmortalObject<T>);

            //ImmortalHeap.Remove((nint)p);

            //p->Delete();
            //NativeMemory.Free(p);
        }

        /// <exception cref="OutOfMemoryException">Allocating stackval of memory failed.</exception>
        public stackval* AllocValue(CallFrame frame)
        {
            var allocator = allocatorPool.Rent<stackval>(out var p, AllocationKind.no_reference, frame);

            Stats.total_allocations++;
            Stats.total_bytes_requested += allocator.TotalSize;

            TemporaryHeap.AddLast((nint)p);
            InsertDebugData(new((ulong)sizeof(stackval),
                nameof(AllocValue), (nint)p));

            return p;
        }


        public stackval* AllocateStack(CallFrame frame, int size)
        {
            var allocator = allocatorPool.RentArray<stackval>(out var p, size, frame);
            vm.println($"Allocated stack '{size}' for '{frame.method}'");

            Stats.total_allocations++;
            Stats.total_bytes_requested += allocator.TotalSize;

            InsertDebugData(new((ulong)allocator.TotalSize,
                nameof(AllocateStack), (nint)p));

            ImmortalHeap.AddLast((nint)p);

            return p;
        }

        public void FreeStack(CallFrame frame, stackval* stack, int size)
        {
            ImmortalHeap.Remove((nint)stack);
            DeleteDebugData((nint)stack);
            Stats.total_allocations--;
            Stats.total_bytes_requested -= allocatorPool.Return(stack);
        }

        public void UnsafeAllocValueInto(VeinClass @class, stackval* pointer)
        {
            if (!@class.IsPrimitive)
                return;
            pointer->type = @class.TypeCode;
            pointer->data.l = 0;
        }

        /// <exception cref="OutOfMemoryException">Allocating stackval of memory failed.</exception>
        public stackval* AllocValue(VeinClass @class, CallFrame frame)
        {
            if (!@class.IsPrimitive)
                return null;
            var p = AllocValue(frame);
            p->type = @class.TypeCode;
            p->data.l = 0;
            return p;
        }

        public void FreeValue(stackval* value)
        {
            TemporaryHeap.Remove((nint)value);
            DeleteDebugData((nint)value);
            Stats.total_allocations--;
            Stats.total_bytes_requested -= allocatorPool.Return(value);
        }

        public void FreeArray(IshtarArray* array, CallFrame frame)
        {
            DeleteDebugData((nint)array);
            ArrayRefsHeap.Remove((nint)array);
            Stats.total_bytes_requested -= allocatorPool.Return(array);
            Stats.total_allocations--;
            Stats.alive_objects--;
        }

        public IshtarArray* AllocArray(RuntimeIshtarClass @class, ulong size, byte rank, IshtarObject** node, CallFrame frame)
        {
            if (!@class.is_inited)
                @class.init_vtable(vm);

            if (size >= IshtarArray.MAX_SIZE)
            {
                vm.FastFail(WNE.OVERFLOW, "", frame);
                return null;
            }

            if (rank != 1)
            {
                vm.FastFail(WNE.TYPE_LOAD, "Currently array rank greater 1 not supported.", frame);
                return null;
            }


            var arr = TYPE_ARRAY.AsRuntimeClass(vm.Types);
            var bytes_len = @class.computed_size * size * rank;

            // enter critical zone
            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);

            if (!arr.is_inited) arr.init_vtable(vm);

            var obj = AllocObject(arr, frame, node);

            var allocator = allocatorPool.Rent<IshtarArray>(out var arr_obj, AllocationKind.reference, frame);

            if (arr_obj is null)
            {
                vm.FastFail(WNE.OUT_OF_MEMORY, "", frame);
                return null;
            }

            // validate fields
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!value");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!block");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!size");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!rank");

            // fill array block
            arr_obj->SetMemory(obj);
            arr_obj->element_clazz = IshtarUnsafe.AsPointer(ref @class);
            arr_obj->_block.offset_value = arr.Field["!!value"].vtable_offset;
            arr_obj->_block.offset_block = arr.Field["!!block"].vtable_offset;
            arr_obj->_block.offset_size = arr.Field["!!size"].vtable_offset;
            arr_obj->_block.offset_rank = arr.Field["!!rank"].vtable_offset;

            // fill live table memory
            obj->vtable[arr_obj->_block.offset_value] = (void**)allocator.AllocZeroed(bytes_len, AllocationKind.no_reference, frame);
            obj->vtable[arr_obj->_block.offset_block] = (long*)@class.computed_size;
            obj->vtable[arr_obj->_block.offset_size] = (long*)size;
            obj->vtable[arr_obj->_block.offset_rank] = (long*)rank;

            // fill array block memory
            for (var i = 0UL; i != size; i++)
                ((void**)obj->vtable[arr.Field["!!value"].vtable_offset])[i] = AllocObject(@class, frame, &obj);

            // exit from critical zone
            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);


            InsertDebugData(new(checked((ulong)allocator.TotalSize),
                nameof(AllocArray), (nint)arr_obj));

#if DEBUG
            arr_obj->__gc_id = (long)Stats.alive_objects++;
#else
            GCStats.alive_objects++;
#endif


            Stats.total_bytes_requested += allocator.TotalSize;
            Stats.total_allocations++;

            ArrayRefsHeap.AddLast((nint)arr_obj);

            return arr_obj;
        }


        private readonly Dictionary<RuntimeToken, nint> _types_cache = new();
        private readonly Dictionary<RuntimeToken, Dictionary<string, nint>> _fields_cache = new();
        private readonly Dictionary<RuntimeToken, Dictionary<string, nint>> _methods_cache = new();

        public IshtarObject* AllocTypeInfoObject(RuntimeIshtarClass @class, CallFrame frame)
        {
            if (!@class.is_inited)
                @class.init_vtable(frame.vm);

            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);

            if (_types_cache.TryGetValue(@class.runtime_token, out nint value))
                return (IshtarObject*)value;

            var tt = KnowTypes.Type(frame);
            var obj = AllocObject(tt, frame);
            var gc = frame.GetGC();

            obj->flags |= GCFlags.IMMORTAL;

            obj->vtable[tt.Field["_unique_id"].vtable_offset] = gc.ToIshtarObject(@class.runtime_token.ClassID, frame);
            obj->vtable[tt.Field["_module_id"].vtable_offset] = gc.ToIshtarObject(@class.runtime_token.ModuleID, frame);
            obj->vtable[tt.Field["_flags"].vtable_offset] = gc.ToIshtarObject((int)@class.Flags, frame);
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(@class.Name, frame);
            obj->vtable[tt.Field["_namespace"].vtable_offset] = gc.ToIshtarObject(@class.Path, frame);

            _types_cache[@class.runtime_token] = (nint)obj;

            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);

            return obj;
        }

        public IshtarObject* AllocFieldInfoObject(RuntimeIshtarField field, CallFrame frame)
        {
            var @class = field.Owner as RuntimeIshtarClass;
            if (!@class.is_inited)
                @class.init_vtable(vm);

            var name = field.Name;
            var gc = frame.GetGC();

            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_FIELD_BARRIER);

            if (_fields_cache.ContainsKey(@class.runtime_token) && _fields_cache[@class.runtime_token].ContainsKey(name))
                return (IshtarObject*)_fields_cache[@class.runtime_token][name];

            var tt = KnowTypes.Field(frame);
            var obj = AllocObject(tt, frame);

            obj->flags |= GCFlags.IMMORTAL;

            var field_owner = AllocTypeInfoObject(@class, frame);

            obj->vtable[tt.Field["_target"].vtable_offset] = field_owner;
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(name, frame);
            obj->vtable[tt.Field["_vtoffset"].vtable_offset] = gc.ToIshtarObject((long)field.vtable_offset, frame);

            if (!_fields_cache.ContainsKey(@class.runtime_token))
                _fields_cache[@class.runtime_token] = new();
            _fields_cache[@class.runtime_token][name] = (nint)obj;

            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_FIELD_BARRIER);

            return obj;
        }


        public IshtarObject* AllocMethodInfoObject(RuntimeIshtarMethod method, CallFrame frame)
        {
            var @class = method.Owner as RuntimeIshtarClass;
            if (!@class.is_inited)
                @class.init_vtable(vm);

            var key = method.Name;
            var gc = frame.GetGC();

            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

            if (_fields_cache.ContainsKey(@class.runtime_token) && _fields_cache[@class.runtime_token].ContainsKey(key))
                return (IshtarObject*)_fields_cache[@class.runtime_token][key];

            var tt = KnowTypes.Function(frame);
            var obj = AllocObject(tt, frame);

            obj->flags |= GCFlags.IMMORTAL;

            var method_owner = AllocTypeInfoObject(@class, frame);

            obj->vtable[tt.Field["_target"].vtable_offset] = method_owner;
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(method.RawName, frame);
            obj->vtable[tt.Field["_quality_name"].vtable_offset] = gc.ToIshtarObject(method.Name, frame);
            obj->vtable[tt.Field["_vtoffset"].vtable_offset] = gc.ToIshtarObject((long)method.vtable_offset, frame);

            if (!_methods_cache.ContainsKey(@class.runtime_token))
                _methods_cache[@class.runtime_token] = new();
            _methods_cache[@class.runtime_token][key] = (nint)obj;

            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

            return obj;
        }

        public IshtarObject* AllocObject(RuntimeIshtarClass @class, CallFrame frame, IshtarObject** node = null)
        {
            var allocator = allocatorPool.Rent<IshtarObject>(out var p, AllocationKind.reference, frame);

            p->vtable = (void**)allocator.AllocZeroed((nuint)(sizeof(void*) * (long)@class.computed_size), AllocationKind.reference, frame);

            Unsafe.CopyBlock(p->vtable, @class.vtable, (uint)@class.computed_size * (uint)sizeof(void*));
            p->clazz = IshtarUnsafe.AsPointer(ref @class);
            p->vtable_size = (uint)@class.computed_size;
#if DEBUG
            p->__gc_id = (long)Stats.alive_objects++;
            p->m1 = IshtarObject.magic1;
            p->m2 = IshtarObject.magic2;
            IshtarObject.CreationTrace[p->__gc_id] = Environment.StackTrace;
            var st = new StackTrace(true);
#else
            Stats.alive_objects++;
#endif
            @class.computed_size = @class.computed_size;

            if (node is null || *node is null) fixed (IshtarObject** o = &root)
                    p->owner = o;
            else
                p->owner = node;

            Stats.total_allocations++;
            Stats.total_bytes_requested += allocator.TotalSize;

            InsertDebugData(new(checked((ulong)allocator.TotalSize), nameof(AllocObject), (nint)p));
            RefsHeap.AddLast((nint)p);

            return p;
        }

        public void FreeObject(IshtarObject* obj, CallFrame frame)
        {
            if (!obj->IsValidObject())
            {
                VM.FastFail(WNE.STATE_CORRUPT, "trying free memory of invalid object", frame);
                return;
            }

            if (obj->flags.HasFlag(GCFlags.NATIVE_REF))
            {
                VM.FastFail(WNE.ACCESS_VIOLATION, "trying free memory of static native object", frame);
                return;
            }
            DeleteDebugData((nint)obj);
            RefsHeap.Remove((nint)obj);

            if (obj->clazz != null)
                GCHandle.FromIntPtr((nint)obj->clazz).Free();

            Stats.total_bytes_requested -= allocatorPool.Return(obj);
            Stats.total_allocations--;
            Stats.alive_objects--;
        }

        public void FreeObject(IshtarObject** obj, CallFrame frame) => FreeObject(*obj, frame);
    }
}
