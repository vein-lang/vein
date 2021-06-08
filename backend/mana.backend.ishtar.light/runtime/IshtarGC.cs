namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using mana.extensions;
    using mana.runtime;
    using static mana.runtime.ManaTypeCode;

    public static unsafe class IshtarGC
    {
        private static readonly SortedSet<nint> heap = new();
        public static class GCStats
        {
            public static ulong total_allocations;
            public static ulong total_bytes_requested;
            public static ulong alive_objects;
        }

        public static IshtarObject* root;

        public static void INIT()
        {
            if (root is not null)
                return;
            var p = (IshtarObject*) Marshal.AllocHGlobal(sizeof(IshtarObject));
            GCStats.total_bytes_requested += (ulong)sizeof(IshtarObject);
            GCStats.alive_objects++;
            GCStats.total_allocations++;
            root = p;
        }

        public static stackval* AllocValue()
        {
            var p = (stackval*) Marshal.AllocHGlobal(sizeof(stackval));
            GCStats.total_allocations++;
            GCStats.total_bytes_requested += (ulong)sizeof(stackval);
            return p;
        }
        public static stackval* AllocValue(ManaClass @class)
        {
            if (!@class.IsPrimitive)
                return null;
            var p = AllocValue();
            p->type = @class.TypeCode;
            return p;
        }


        public static IshtarArray* AllocArray(RuntimeIshtarClass @class, ulong size, byte rank, IshtarObject** node = null, CallFrame frame = null)
        {
            if (!@class.is_inited)
                @class.init_vtable();

            if (size >= IshtarArray.MAX_SIZE)
            {
                VM.FastFail(WNE.OVERFLOW, "", frame);
                VM.ValidateLastError();
                return null;
            }

            if (rank != 1)
            {
                VM.FastFail(WNE.TYPE_LOAD, "Currently array rank greater 1 not supported.", frame);
                VM.ValidateLastError();
                return null;
            }
            var arr = TYPE_ARRAY.AsRuntimeClass();
            var bytes_len = @class.computed_size * size * rank;

            // enter critical zone
            IshtarSync.EnterCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);
            
            if (!arr.is_inited) arr.init_vtable();

            var obj = AllocObject(arr, node);

            var arr_obj = (IshtarArray*)Marshal.AllocHGlobal(sizeof(IshtarArray));

            if (arr_obj is null)
            {
                VM.FastFail(WNE.OUT_OF_MEMORY, "", frame);
                VM.ValidateLastError();
                return null;
            }

            // validate fields
            FFI.StaticValidateField(frame, &obj, "!!value");
            FFI.StaticValidateField(frame, &obj, "!!block");
            FFI.StaticValidateField(frame, &obj, "!!size");
            FFI.StaticValidateField(frame, &obj, "!!rank");

            // fill array block
            arr_obj->memory = obj;
            arr_obj->_block.offset_value = arr.Field["!!value"].vtable_offset;
            arr_obj->_block.offset_block = arr.Field["!!block"].vtable_offset;
            arr_obj->_block.offset_size = arr.Field["!!size"].vtable_offset;
            arr_obj->_block.offset_rank = arr.Field["!!rank"].vtable_offset;

            // update gc stats
            GCStats.alive_objects++;
            GCStats.total_allocations += (ulong)sizeof(IshtarArray) + bytes_len;


            // fill live table memory
            obj->vtable[arr_obj->_block.offset_value] = (void**) Marshal.AllocHGlobal((IntPtr)bytes_len);
            obj->vtable[arr_obj->_block.offset_block] = (long*) @class.computed_size;
            obj->vtable[arr_obj->_block.offset_size] = (long*) size;
            obj->vtable[arr_obj->_block.offset_rank] = (long*) rank;

            // fill array block memory
            for (var i = 0UL; i != size; i++)
                ((void**) obj->vtable[arr.Field["!!value"].vtable_offset])[i] = AllocObject(@class, &obj);

            // exit from critical zone
            IshtarSync.LeaveCriticalSection(ref @class.Owner.Interlocker.INIT_ARRAY_BARRIER);

            return arr_obj;
        }



        public static IshtarObject* AllocObject(RuntimeIshtarClass @class, IshtarObject** node = null)
        {
            var p = (IshtarObject*) Marshal.AllocHGlobal(sizeof(IshtarObject));

            Unsafe.InitBlock(p, 0, (uint)sizeof(IshtarObject));


            p->vtable = (void**)Marshal.AllocHGlobal(new IntPtr(sizeof(void*) * (long)@class.computed_size));
            Unsafe.CopyBlock(p->vtable, @class.vtable, (uint)@class.computed_size * (uint)sizeof(void*));
            p->clazz = IshtarUnsafe.AsPointer(ref @class);

            GCStats.alive_objects++;
            GCStats.total_allocations++;
            GCStats.total_bytes_requested += @class.computed_size * (ulong)sizeof(void*);
            GCStats.total_bytes_requested += (ulong)sizeof(IshtarObject);

            if (node is null || *node is null)
                fixed (IshtarObject** o = &root)
                    p->owner = o;
            else
                p->owner = node;

            return p;
        }
        public static void FreeObject(IshtarObject** obj)
        {
            Marshal.FreeHGlobal(new IntPtr((*obj)->vtable));
            (*obj)->vtable = null;
            (*obj)->clazz = null;
            Marshal.FreeHGlobal((nint)(*obj));
            GCStats.alive_objects--;
        }
    }
}
