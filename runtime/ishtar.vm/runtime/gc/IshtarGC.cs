namespace ishtar.runtime.gc
{
    using allocators;
    using ishtar;
    using collections;
    using libuv;
    using vein.runtime;
    using static vein.runtime.VeinTypeCode;
    using static libuv.LibUV;


    public unsafe struct GCSync(uv_mutex_t* g) : IDisposable
    {
        private bool lockTaken;

        public static IDisposable Begin(uv_mutex_t* g) => new GCSync(g).begin();

        private IDisposable begin()
        {
            uv_mutex_lock(g);
            lockTaken = true;
            return this;
        }

        public void Dispose()
        {
            if (lockTaken)
            {
                uv_mutex_unlock(g);
                lockTaken = false;
            }
        }

        public static uv_mutex_t* create()
        {
            var al = IshtarGC.AllocateImmortal<uv_mutex_t>(null);
            var err = uv_mutex_init(al);

            if (err != UV_ERR.OK)
                throw new NotSupportedException();

            return al;
        }

        public static void destroy(uv_mutex_t* handle) => uv_mutex_destroy(handle);
    }

    // ReSharper disable once ClassTooBig
    public unsafe struct IshtarGC(VirtualMachine* vm) : IDisposable
    {
        public static IshtarGC* Create(VirtualMachine* v)
        {
            var gc = AllocateImmortal<IshtarGC>(v);
            *gc = new IshtarGC(v);

            gc->mutex = GCSync.create();
            return gc;
        }



        private static readonly IIshtarAllocatorPool allocatorPool
            = new IshtarAllocatorPool(gcLayout = new BoehmGCLayout());
#if BOEHM_GC
        private static readonly GCLayout gcLayout;
#else
        private static GCLayout gcLayout = null;
#error No defined GC layout
#endif
        public VirtualMachine* VM => vm;
        private readonly bool check_memory_leak = true;

        private uv_mutex_t* mutex;

        private bool is_disposed;

        private ulong TotalAllocation;
        private ulong TotalBytesAllocated;
        public ulong alive_objects;

#if DEBUG
        public static string debug_previousDisposeStackTrace;
#endif

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


        public void Dispose()
        {
            if (is_disposed)
                throw new InvalidOperationException();
            is_disposed = true;
#if DEBUG
            debug_previousDisposeStackTrace = Environment.StackTrace;
#endif
            var gcHeapSizeBefore = gcLayout.get_heap_size();
            var gcFreeBytesBefore = gcLayout.get_free_bytes();
            var gcHeapUsageBefore = gcLayout.get_heap_usage();

            gcLayout.collect();
            //var hasCollected = gcLayout.try_collect();
            gcLayout.finalize_all();

            //foreach (var p in RefsHeap.ToArray())
            //{
            //    FreeObject((IshtarObject*)p, VM.Frames->GarbageCollector);
            //}
            //RefsHeap.Clear();

            //foreach (var p in ArrayRefsHeap.ToArray())
            //{
            //    FreeArray((IshtarArray*)p, VM.Frames->GarbageCollector);
            //}
            //ArrayRefsHeap.Clear();

            //hasCollected = gcLayout.try_collect();

            if (!check_memory_leak) return;

            var gcHeapSize = gcLayout.get_heap_size();
            var gcFreeBytes = gcLayout.get_free_bytes();
            var gcHeapUsage = gcLayout.get_heap_usage();

            if (gcHeapUsage.pbytes_since_gc != 0)
            {
                vm->FastFail(WNE.MEMORY_LEAK, $"After clear all allocated memory, total_bytes_requested is not zero ({total_bytes_requested})", VM->Frames->GarbageCollector);
                return;
            }
        }


        /// <exception cref="OutOfMemoryException">Allocating stackval of memory failed.</exception>
        public stackval* AllocValue(CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.Rent<stackval>(out var p, AllocationKind.no_reference, frame);

            total_allocations++;
            total_bytes_requested += allocator.TotalSize;

            return p;
        }

        /// <exception cref="OutOfMemoryException">Allocating stackval of memory failed.</exception>
        public rawval* AllocRawValue(CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.Rent<rawval>(out var p, AllocationKind.reference, frame);
            
            total_allocations++;
            total_bytes_requested += allocator.TotalSize;

            return p;
        }


        public stackval* AllocateStack(CallFrame* frame, int size)
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.RentArray<stackval>(out var p, size, frame);
            vm->println($"Allocated stack '{size}' for '{frame->method->Name}'");

            total_allocations++;
            total_bytes_requested += allocator.TotalSize;
            
            return p;
        }

        public void FreeStack(CallFrame* frame, stackval* stack, int size)
        {
            using var _ = GCSync.Begin(mutex);

            total_allocations--;
            total_bytes_requested -= allocatorPool.Return(stack);
        }

        public void UnsafeAllocValueInto(RuntimeIshtarClass* @class, stackval* pointer)
        {
            using var _ = GCSync.Begin(mutex);

            if (!@class->IsPrimitive)
                return;
            pointer->type = @class->TypeCode;
            pointer->data.l = 0;
        }

        /// <exception cref="OutOfMemoryException">Allocating stackval of memory failed.</exception>
        public stackval* AllocValue(VeinClass @class, CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            if (!@class.IsPrimitive)
                return null;
            var p = AllocValue(frame);
            p->type = @class.TypeCode;
            p->data.l = 0;
            return p;
        }

        public T* AllocateSystemStruct<T>(CallFrame* frame) where T : unmanaged
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.Rent<T>(out var p, AllocationKind.reference, frame);

            total_allocations++;
            total_bytes_requested += allocator.TotalSize;

            return p;
        }

        public T* AllocateUVStruct<T>(CallFrame* frame) where T : unmanaged
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.Rent<nint>(out var p, AllocationKind.reference, frame);

            total_allocations++;
            total_bytes_requested += allocator.TotalSize;

            return (T*)p;
        }

        public void FreeRawValue(rawval* value)
        {
            using var _ = GCSync.Begin(mutex);

            total_allocations--;
            total_bytes_requested -= allocatorPool.Return(value);
        }

        public void FreeValue(stackval* value)
        {
            using var _ = GCSync.Begin(mutex);

            total_allocations--;
            total_bytes_requested -= allocatorPool.Return(value);
        }

        public void FreeArray(IshtarArray* array, CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            total_bytes_requested -= allocatorPool.Return(array);
            total_allocations--;
            alive_objects--;
        }

        public IshtarArray* AllocArray(RuntimeIshtarClass* @class, ulong size, byte rank, CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            if (!@class->is_inited)
                throw new NotImplementedException();

            if (size >= IshtarArray.MAX_SIZE)
            {
                vm->FastFail(WNE.OVERFLOW, "", frame);
                return null;
            }

            if (rank != 1)
            {
                vm->FastFail(WNE.TYPE_LOAD, "Currently array rank greater 1 not supported.", frame);
                return null;
            }


            var arr = TYPE_ARRAY.AsRuntimeClass(vm->Types);
            var bytes_len = @class->computed_size * size * rank;

            // enter critical zone
            //IshtarSync.EnterCriticalSection(ref @class->Owner->Interlocker.INIT_ARRAY_BARRIER);

            if (!arr->is_inited) arr->init_vtable(vm);

            var obj = AllocObject(arr, frame);

            var allocator = allocatorPool.Rent<IshtarArray>(out var arr_obj, AllocationKind.reference, frame);

            if (arr_obj is null)
            {
                vm->FastFail(WNE.OUT_OF_MEMORY, "", frame);
                return null;
            }

            // validate fields
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!value");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!block");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!size");
            ForeignFunctionInterface.StaticValidateField(frame, &obj, "!!rank");

            // fill array block
            arr_obj->SetMemory(obj);
            arr_obj->element_clazz = @class;
            arr_obj->_block.offset_value = arr->Field["!!value"]->vtable_offset;
            arr_obj->_block.offset_block = arr->Field["!!block"]->vtable_offset;
            arr_obj->_block.offset_size = arr->Field["!!size"]->vtable_offset;
            arr_obj->_block.offset_rank = arr->Field["!!rank"]->vtable_offset;

            // fill live table memory
            obj->vtable[arr_obj->_block.offset_value] = (void**)allocator.AllocZeroed(bytes_len, AllocationKind.no_reference, frame);
            obj->vtable[arr_obj->_block.offset_block] = (long*)@class->computed_size;
            obj->vtable[arr_obj->_block.offset_size] = (long*)size;
            obj->vtable[arr_obj->_block.offset_rank] = (long*)rank;

            // fill array block memory
            for (var i = 0UL; i != size; i++)
                ((void**)obj->vtable[arr->Field["!!value"]->vtable_offset])[i] = AllocObject(@class, frame);

            // exit from critical zone
            //IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);


#if DEBUG
            arr_obj->__gc_id = (long)alive_objects++;
#else
            GCStats.alive_objects++;
#endif

            total_bytes_requested += allocator.TotalSize;
            total_allocations++;

            return arr_obj;
        }

        
        // TODO
        public void** AllocVTable(uint size)
        {
            var p = (void**)gcLayout.alloc((uint)(size * sizeof(void*)));

            if (p is null)
                vm->FastFail(WNE.TYPE_LOAD, "Out of memory.", vm->Frames->GarbageCollector);
            return p;
        }

        //public IshtarObject* AllocTypeInfoObject(RuntimeIshtarClass* @class, CallFrame* frame)
        //{
        //    using var _ = GCSync.Begin(this);

        //    if (!@class->is_inited)
        //        throw new NotImplementedException();

        //    //IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);

        //    if (_types_cache.TryGetValue(@class->runtime_token, out nint value))
        //        return (IshtarObject*)value;

        //    var tt = KnowTypes.Type(frame);
        //    var obj = AllocObject(tt, frame);
        //    var gc = frame->GetGC();

        //    obj->flags |= GCFlags.IMMORTAL;

        //    obj->vtable[tt->Field["_unique_id"]->vtable_offset] = gc->ToIshtarObjectT(@class->runtime_token.ClassID, frame);
        //    obj->vtable[tt->Field["_module_id"]->vtable_offset] = gc->ToIshtarObjectT(@class->runtime_token.ModuleID, frame);
        //    obj->vtable[tt->Field["_flags"]->vtable_offset] = gc->ToIshtarObject((int)@class->Flags, frame);
        //    obj->vtable[tt->Field["_name"]->vtable_offset] = gc->ToIshtarObject(@class->Name, frame);
        //    obj->vtable[tt->Field["_namespace"]->vtable_offset] = gc->ToIshtarObject(@class->FullName->Namespace, frame);

        //    _types_cache[@class->runtime_token] = (nint)obj;

        //    //IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);

        //    return obj;
        //}

        //public IshtarObject* AllocFieldInfoObject(RuntimeIshtarField* field, CallFrame* frame)
        //{
        //    using var _ = GCSync.Begin(this);

        //    var @class = field->Owner;
        //    if (!@class->is_inited)
        //        throw new NotImplementedException();

        //    var name = field->Name;
        //    var gc = frame->GetGC();

        //    //IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_FIELD_BARRIER);

        //    if (_fields_cache.ContainsKey(@class->runtime_token) && _fields_cache[@class->runtime_token].ContainsKey(name))
        //        return (IshtarObject*)_fields_cache[@class->runtime_token][name];

        //    var tt = KnowTypes.Field(frame);
        //    var obj = AllocObject(tt, frame);

        //    obj->flags |= GCFlags.IMMORTAL;

        //    var field_owner = AllocTypeInfoObject(@class, frame);

        //    obj->vtable[tt->Field["_target"]->vtable_offset] = field_owner;
        //    obj->vtable[tt->Field["_name"]->vtable_offset] = gc->ToIshtarObject(name, frame);
        //    obj->vtable[tt->Field["_vtoffset"]->vtable_offset] = gc->ToIshtarObject((long)field->vtable_offset, frame);

        //    if (!_fields_cache.ContainsKey(@class->runtime_token))
        //        _fields_cache[@class->runtime_token] = new();
        //    _fields_cache[@class->runtime_token][name] = (nint)obj;

        //    //IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_FIELD_BARRIER);

        //    return obj;
        //}


        //public IshtarObject* AllocMethodInfoObject(RuntimeIshtarMethod* method, CallFrame* frame)
        //{
        //    using var _ = GCSync.Begin(this);

        //    var @class = method->Owner;
        //    if (!@class->is_inited)
        //        throw new NotImplementedException();

        //    var key = method->Name;
        //    var gc = frame->GetGC();

        //    //IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

        //    if (_fields_cache.ContainsKey(@class->runtime_token) && _fields_cache[@class->runtime_token].ContainsKey(key))
        //        return (IshtarObject*)_fields_cache[@class->runtime_token][key];

        //    var tt = KnowTypes.Function(frame);
        //    var obj = AllocObject(tt, frame);

        //    obj->flags |= GCFlags.IMMORTAL;

        //    var method_owner = AllocTypeInfoObject(@class, frame);

        //    obj->vtable[tt->Field["_target"]->vtable_offset] = method_owner;
        //    obj->vtable[tt->Field["_name"]->vtable_offset] = gc->ToIshtarObject(method->RawName, frame);
        //    obj->vtable[tt->Field["_quality_name"]->vtable_offset] = gc->ToIshtarObject(method->Name, frame);
        //    obj->vtable[tt->Field["_vtoffset"]->vtable_offset] = gc->ToIshtarObject((long)method->vtable_offset, frame);

        //    if (!_methods_cache.ContainsKey(@class->runtime_token))
        //        _methods_cache[@class->runtime_token] = new();
        //    _methods_cache[@class->runtime_token][key] = (nint)obj;

        //    //IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

        //    return obj;
        //}

        public IshtarObject* AllocObject(RuntimeIshtarClass* @class, CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            var allocator = allocatorPool.Rent<IshtarObject>(out var p,
                AllocationKind.reference, frame);

            if (!@class->is_inited)
                throw new NotImplementedException();

            p->vtable = (void**)allocator.AllocZeroed(
                (nuint)(sizeof(void*) * (long)@class->computed_size),
                AllocationKind.reference, frame);

            IshtarUnsafe.CopyBlock(p->vtable, @class->vtable,
                (uint)@class->computed_size * (uint)sizeof(void*));
            p->clazz = @class;
            p->vtable_size = (uint)@class->computed_size;
            p->__gc_id = (long)alive_objects++;
            #if DEBUG
            p->m1 = IshtarObject.magic1;
            p->m2 = IshtarObject.magic2;
            IshtarObject.CreationTrace[p->__gc_id] = Environment.StackTrace;
            #endif
            @class->computed_size = @class->computed_size;

            total_allocations++;
            total_bytes_requested += allocator.TotalSize;
            
            ObjectRegisterFinalizer(p, &_direct_finalizer, frame);

            return p;
        }


        private static void _direct_finalizer(nint obj, nint __)
        {
            var o = (IshtarObject*)obj;

            if (!o->IsValidObject())
            {
                #if DEBUG
                Debug.WriteLine($"Detected invalid object when calling _direct_finalizer");
                Debugger.Break();
                #endif
                return;
            }

            var vm = o->clazz->Owner->vm;
            var gc = vm->gc;

            using var _ = GCSync.Begin(gc->mutex);

            var frame = vm->Frames->GarbageCollector;

            gc->ObjectRegisterFinalizer(o, null, frame);

            if (vm->Config.DisabledFinalization)
                return;

            var clazz = o->clazz;

            var finalizer = clazz->GetDefaultDtor();

            vm->println($"@@[dtor] called! for instance of {clazz->FullName->NameWithNS}");
            if (finalizer is not null)
            {
                vm->exec_method(frame->CreateChild(finalizer));
                vm->watcher.ValidateLastError();
            }

            gc->total_allocations--;
            gc->alive_objects--;


            gcLayout.free((void**)&o);
        }


        public void FreeObject(IshtarObject* obj, CallFrame* frame)
        {
            using var _ = GCSync.Begin(mutex);

            if (obj->IsDestroyedObject())
            {
                gcLayout.register_finalizer_no_order(obj, null, frame);
                total_bytes_requested -= allocatorPool.Return(obj);
                total_allocations--;
                alive_objects--;
                return;
            }

            if (!obj->IsValidObject())
            {
                vm->FastFail(WNE.STATE_CORRUPT, "trying free memory of invalid object", frame);
                return;
            }

            if (obj->flags.HasFlag(GCFlags.NATIVE_REF))
            {
                vm->FastFail(WNE.ACCESS_VIOLATION, "trying free memory of static native object", frame);
                return;
            }
            gcLayout.register_finalizer_no_order(obj, null, frame);
            
            total_bytes_requested -= allocatorPool.Return(obj);
            total_allocations--;
            alive_objects--;

            gcLayout.free((void**)&obj);
        }

        public bool IsAlive(IshtarObject* obj)
        {
            using var _ = GCSync.Begin(mutex);

            return obj->IsValidObject() && gcLayout.is_marked(obj);
        }

        public void ObjectRegisterFinalizer(IshtarObject* obj, delegate*<nint, nint, void> proc, CallFrame* frame)
        {
            var clazz = obj->clazz;

            //IshtarSync.EnterCriticalSection(ref clazz.Owner.Interlocker.GC_FINALIZER_BARRIER);

            gcLayout.register_finalizer_no_order(obj, proc, frame);

            //IshtarSync.LeaveCriticalSection(ref clazz.Owner.Interlocker.GC_FINALIZER_BARRIER);
        }
        
        public void RegisterWeakLink(IshtarObject* obj, void** link, bool longLive)
            => gcLayout.create_weak_link(link, obj, longLive);
        public void UnRegisterWeakLink(void** link, bool longLive)
            => gcLayout.unlink(link, longLive);

        public void FreeObject(IshtarObject** obj, CallFrame* frame) => FreeObject(*obj, frame);

        public long GetUsedMemorySize() => gcLayout.get_heap_size() - gcLayout.get_free_bytes();

        public void Collect() => gcLayout.collect();

        

        public void register_thread(GC_stack_base* attr) => gcLayout.register_thread(attr);
        public void unregister_thread() => gcLayout.unregister_thread();
        public bool get_stack_base(GC_stack_base* attr) => gcLayout.get_stack_base(attr);

        #region internal

        public static AllocatorBlock CreateAllocatorWithParent(void* parent)  =>
            new(parent, &IshtarGC_Free, &IshtarGC_Realloc, &AllocateImmortal, &AllocateImmortal);

        private static void IshtarGC_Free(void* ptr)
            => BoehmGCLayout.Native.GC_free(ptr);

        private static void* IshtarGC_Realloc(void* ptr, uint newBytes)
            => (void*)BoehmGCLayout.Native.GC_realloc((nint)ptr, newBytes);


        public static NativeList<T>* AllocateList<T>(void* parent, int initialCapacity = 16) where T : unmanaged, IEq<T> 
            => NativeList<T>.Create(initialCapacity, CreateAllocatorWithParent(parent));


        public static void FreeList<T>(NativeList<T>* list) where T : unmanaged, IEq<T>
            => NativeList<T>.Free(list);


        public static AtomicNativeList<T>* AllocateAtomicList<T>(void* parent, int initialCapacity = 16) 
            where T : unmanaged, IEquatable<T>
            => AtomicNativeList<T>.Create(initialCapacity, CreateAllocatorWithParent(parent));

        public static NativeDictionary<TKey, TValue>* AllocateDictionary<TKey, TValue>(void* parent, int initialCapacity = 16) 
            where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
            => NativeDictionary<TKey, TValue>.Create(initialCapacity, CreateAllocatorWithParent(parent));

        public static AtomicNativeDictionary<TKey, TValue>* AllocateAtomicDictionary<TKey, TValue>(void* parent, int initialCapacity = 16)
            where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged, IEquatable<TValue>
            => AtomicNativeDictionary<TKey, TValue>.Create(initialCapacity, CreateAllocatorWithParent(parent));

        public static void FreeDictionary<TKey, TValue>(NativeDictionary<TKey, TValue>* list)
            where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
            => NativeDictionary<TKey, TValue>.Free(list);

        public static NativeQueue<T>* AllocateQueue<T>(void* parent, int initialCapacity = 16) 
            where T : unmanaged, IEq<T>
            => NativeQueue<T>.Create(initialCapacity, CreateAllocatorWithParent(parent));

        public static void FreeQueue<T>(NativeQueue<T>* queue) where T : unmanaged, IEq<T>
            => NativeQueue<T>.Free(queue);

        public static NativeConcurrentDictionary<TKey, TValue>* AllocateConcurrentDictionary<TKey, TValue>(void* parent, int initialCapacity = 16) 
            where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
            => NativeConcurrentDictionary<TKey, TValue>.Create(initialCapacity, CreateAllocatorWithParent(parent));

        public static void FreeConcurrentDictionary<TKey, TValue>(NativeConcurrentDictionary<TKey, TValue>* list)
            where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
            => NativeConcurrentDictionary<TKey, TValue>.Free(list);
        
        public static T* AllocateImmortal<T>(void* parent) where T : unmanaged 
        {
            var p = (T*)gcLayout.alloc_immortal((uint)sizeof(T));
            allocatedImmortals.Add((nint)p);
            return p;
        }


        public static T* AllocateUVImmortal<T>() where T : unmanaged
        {
            var p = (T*)gcLayout.alloc_immortal((uint)sizeof(nint));
            return p;
        }

        public static void* AllocateImmortal(uint size, void* parent)
        {
            var p = gcLayout.alloc_immortal(size);
            allocatedImmortals.Add((nint)p);
            return p;
        }

        public static T* AllocateImmortalRoot<T>() where T : unmanaged
        {
            var t = (T*)gcLayout.alloc_immortal((uint)sizeof(T));
            gcLayout.add_roots(t, sizeof(T));
            return t;
        }

        public static void FreeImmortalRoot<T>(T* ptr) where T : unmanaged
        {
            gcLayout.remove_roots(ptr, sizeof(T));
            gcLayout.free((void**)&ptr);
        }

        public static T* AllocateImmortal<T>(int size, void* parent) where T : unmanaged
        {
            var p = (T*)gcLayout.alloc_immortal((uint)(sizeof(T) * size));

            allocatedImmortals.Add((nint)p);

            return p;
        }

        public static T* AllocateImmortalRoot<T>(int size) where T : unmanaged
        {
            var t = (T*)gcLayout.alloc_immortal((uint)(sizeof(T) * size));
            gcLayout.add_roots(t, sizeof(T) * size);
            return t;
        }

        public static void FreeImmortalRoot<T>(T* ptr, int size) where T : unmanaged
        {
            gcLayout.remove_roots(ptr, sizeof(T) * size);
            gcLayout.free((void**)&ptr);
        }


        private static readonly List<nint> allocatedImmortals = new();
        private static readonly Dictionary<nint, string> disposedImmortals = new();
        public static void FreeImmortal<T>(T* t) where T : unmanaged
        {
            lock (typeof(T))
            {
                if (!gcLayout.isOwnerShip((void**)&t))
                {
                    Debug.WriteLine($"Trying free pointer without access");
                    return;
                }

                var stackTrace = Environment.StackTrace;
                if (allocatedImmortals.Remove((nint)t))
                {
                    disposedImmortals[(nint)t] = stackTrace;
                    gcLayout.free((void**)&t);
                }
                else if (disposedImmortals.TryGetValue((nint)t, out var result))
                {
                    if (stackTrace.Equals(result))
                        return;
                    throw new TryingFreeAlreadyDisposedImmortalObject(disposedImmortals[(nint)t]);
                }
                else
                    throw new BadMemoryOfImmortalObject();
            }
        }

        #endregion
    }

    public class BadMemoryOfImmortalObject : Exception;
    public class TryingFreeAlreadyDisposedImmortalObject(string stackTrace) : Exception(stackTrace);
}
