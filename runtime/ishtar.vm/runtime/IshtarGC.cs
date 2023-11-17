namespace ishtar
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using allocators;
    using vein.runtime;
    using static vein.runtime.VeinTypeCode;

    public unsafe class IshtarGC : IDisposable
    {
        private readonly VirtualMachine _vm;
        public readonly GCStats Stats = new();
        private readonly LinkedList<nint> RefsHeap = new();
        private readonly LinkedList<nint> ArrayRefsHeap = new();
        private readonly LinkedList<nint> ImmortalHeap = new();
        private readonly LinkedList<nint> TemporaryHeap = new();
        private readonly LinkedList<ConstantTypeMemory> ConstantTypeHeap = new();

        private readonly LinkedList<AllocationDebugInfo> allocationTreeDebugInfos = new();
        private readonly Dictionary<nint, AllocationDebugInfo> allocationDebugInfos = new();

        private readonly IIshtarAllocatorPool allocatorPool = new IshtarAllocatorPool();

        public class GCStats
        {
            public ulong total_allocations;
            public ulong total_bytes_requested;
            public ulong alive_objects;
        }

        public record AllocationDebugInfo(ulong BytesAllocated, string Method, nint pointer)
        {
            public string Trace;

            public void Bump() => Trace = new System.Diagnostics.StackTrace().ToString();
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

        public IshtarObject* root;
        public VirtualMachine VM => _vm;
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
                _vm.FastFail(WNE.MEMORY_LEAK, $"After clear all allocated memory, total_allocations is not zero ({Stats.total_allocations})", VM.Frames.GarbageCollector());
                return;
            }
            if (Stats.total_bytes_requested != 0)
            {
                _vm.FastFail(WNE.MEMORY_LEAK, $"After clear all allocated memory, total_bytes_requested is not zero ({Stats.total_bytes_requested})", VM.Frames.GarbageCollector());
                return;
            }
        }

        public IshtarGC(VirtualMachine vm)
        {
            _vm = vm;
            if (root is not null)
                return;
            root = AllocObject(TYPE_OBJECT.AsRuntimeClass(VM.Types));
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
        public stackval* AllocValue()
        {
            allocatorPool.Rent<stackval>(out var p);

            Stats.total_allocations++;

            TemporaryHeap.AddLast((nint)p);
            InsertDebugData(new((ulong)sizeof(stackval),
                nameof(AllocValue), (nint)p));

            return p;
        }


        public stackval* AllocateStack(CallFrame frame, int size)
        {
            allocatorPool.RentArray<stackval>(out var p, size);
            _vm.println($"Allocated stack '{size}' for '{frame.method}'");

            Stats.total_allocations++;
            Stats.total_bytes_requested += (ulong)(sizeof(stackval) * size);

            InsertDebugData(new((ulong)(sizeof(stackval) * size),
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
        public stackval* AllocValue(VeinClass @class)
        {
            if (!@class.IsPrimitive)
                return null;
            var p = AllocValue();
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
            Stats.total_allocations--;
            Stats.total_bytes_requested -= allocatorPool.Return(array);
        }

        public IshtarArray* AllocArray(RuntimeIshtarClass @class, ulong size, byte rank, IshtarObject** node = null, CallFrame frame = null)
        {
            if (!@class.is_inited)
                @class.init_vtable(_vm);

            if (size >= IshtarArray.MAX_SIZE)
            {
                _vm.FastFail(WNE.OVERFLOW, "", frame);
                return null;
            }

            if (rank != 1)
            {
                _vm.FastFail(WNE.TYPE_LOAD, "Currently array rank greater 1 not supported.", frame);
                return null;
            }


            var arr = TYPE_ARRAY.AsRuntimeClass(_vm.Types);
            var bytes_len = @class.computed_size * size * rank;

            // enter critical zone
            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);

            if (!arr.is_inited) arr.init_vtable(_vm);

            var obj = AllocObject(arr, node);

            var allocator = allocatorPool.Rent<IshtarArray>(out var arr_obj);
            
            if (arr_obj is null)
            {
                _vm.FastFail(WNE.OUT_OF_MEMORY, "", frame);
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


#if DEBUG
            arr_obj->__gc_id = (long)Stats.alive_objects++;
#else
            GCStats.alive_objects++;
#endif

            
            // fill live table memory
            obj->vtable[arr_obj->_block.offset_value] = (void**)allocator.AllocZeroed(bytes_len);
            obj->vtable[arr_obj->_block.offset_block] = (long*)@class.computed_size;
            obj->vtable[arr_obj->_block.offset_size] = (long*)size;
            obj->vtable[arr_obj->_block.offset_rank] = (long*)rank;

            // fill array block memory
            for (var i = 0UL; i != size; i++)
                ((void**)obj->vtable[arr.Field["!!value"].vtable_offset])[i] = AllocObject(@class, &obj);

            // exit from critical zone
            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);


            InsertDebugData(new(allocator.TotalSize,
                nameof(AllocArray), (nint)arr_obj));

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

            if (_types_cache.ContainsKey(@class.runtime_token))
                return (IshtarObject*)_types_cache[@class.runtime_token];

            var tt = KnowTypes.Type(frame);
            var obj = AllocObject(tt);
            var gc = frame.GetGC();

            obj->flags |= GCFlags.IMMORTAL;

            obj->vtable[tt.Field["_unique_id"].vtable_offset] = gc.ToIshtarObject(@class.runtime_token.ClassID);
            obj->vtable[tt.Field["_module_id"].vtable_offset] = gc.ToIshtarObject(@class.runtime_token.ModuleID);
            obj->vtable[tt.Field["_flags"].vtable_offset] = gc.ToIshtarObject((int)@class.Flags);
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(@class.Name);
            obj->vtable[tt.Field["_namespace"].vtable_offset] = gc.ToIshtarObject(@class.Path);

            _types_cache[@class.runtime_token] = (nint)obj;

            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_TYPE_BARRIER);

            return obj;
        }

        public IshtarObject* AllocFieldInfoObject(RuntimeIshtarField field, CallFrame frame)
        {
            var @class = field.Owner as RuntimeIshtarClass;
            if (!@class.is_inited)
                @class.init_vtable(_vm);

            var name = field.Name;
            var gc = frame.GetGC();

            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_FIELD_BARRIER);

            if (_fields_cache.ContainsKey(@class.runtime_token) && _fields_cache[@class.runtime_token].ContainsKey(name))
                return (IshtarObject*)_fields_cache[@class.runtime_token][name];

            var tt = KnowTypes.Field(frame);
            var obj = AllocObject(tt);

            obj->flags |= GCFlags.IMMORTAL;

            var field_owner = AllocTypeInfoObject(@class, frame);

            obj->vtable[tt.Field["_target"].vtable_offset] = field_owner;
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(name);
            obj->vtable[tt.Field["_vtoffset"].vtable_offset] = gc.ToIshtarObject((long)field.vtable_offset);

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
                @class.init_vtable(_vm);

            var key = method.Name;
            var gc = frame.GetGC();

            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

            if (_fields_cache.ContainsKey(@class.runtime_token) && _fields_cache[@class.runtime_token].ContainsKey(key))
                return (IshtarObject*)_fields_cache[@class.runtime_token][key];

            var tt = KnowTypes.Function(frame);
            var obj = AllocObject(tt);

            obj->flags |= GCFlags.IMMORTAL;

            var method_owner = AllocTypeInfoObject(@class, frame);

            obj->vtable[tt.Field["_target"].vtable_offset] = method_owner;
            obj->vtable[tt.Field["_name"].vtable_offset] = gc.ToIshtarObject(method.RawName);
            obj->vtable[tt.Field["_quality_name"].vtable_offset] = gc.ToIshtarObject(method.Name);
            obj->vtable[tt.Field["_vtoffset"].vtable_offset] = gc.ToIshtarObject((long)method.vtable_offset);

            if (!_methods_cache.ContainsKey(@class.runtime_token))
                _methods_cache[@class.runtime_token] = new();
            _methods_cache[@class.runtime_token][key] = (nint)obj;

            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_METHOD_BARRIER);

            return obj;
        }

        public IshtarObject* AllocObject(RuntimeIshtarClass @class, IshtarObject** node = null)
        {
            var allocator = allocatorPool.Rent<IshtarObject>(out var p);
            
            p->vtable = (void**)allocator.AllocZeroed((nuint)(sizeof(void*) * (long)@class.computed_size));

            Unsafe.CopyBlock(p->vtable, @class.vtable, (uint)@class.computed_size * (uint)sizeof(void*));
            p->clazz = IshtarUnsafe.AsPointer(ref @class);
            p->vtable_size = (uint)@class.computed_size;
#if DEBUG
            p->__gc_id = (long)Stats.alive_objects++;
            p->m1 = IshtarObject.magic1;
            p->m2 = IshtarObject.magic2;
            IshtarObject.CreationTrace[p->__gc_id] = new StackTrace().ToString();
#else
            Stats.alive_objects++;
#endif

            @class.computed_size = @class.computed_size;

            if (node is null || *node is null) fixed (IshtarObject** o = &root)
                    p->owner = o;
            else
                p->owner = node;

            InsertDebugData(new(allocator.TotalSize, nameof(AllocObject), (nint)p));
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

            allocatorPool.Return(obj);

            Stats.total_allocations--;
            Stats.alive_objects--;
        }

        public void FreeObject(IshtarObject** obj, CallFrame frame) => FreeObject(*obj, frame);
    }
}
