namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using wave.runtime;


    public static unsafe class IshtarUnsafe
    {
        public static void* AsPointer<T>(ref T t) where T : class
        {
            var p = GCHandle.Alloc(t, GCHandleType.WeakTrackResurrection);
            return (void*)GCHandle.ToIntPtr(p);
        }

        public static T AsRef<T>(void* raw) where T : class
        {
            var p = GCHandle.FromIntPtr((nint) raw);
            return p.Target as T;
        }
    }
    public static unsafe class IshtarGC
    {
        private static readonly PriorityQueue<nint, int> heap = new();
        private static readonly PriorityQueue<nint, int> val_heap = new();

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

            heap.Enqueue((nint)p, 99);
        }

        public static stackval* AllocValue()
        {
            var p = (stackval*) Marshal.AllocHGlobal(sizeof(stackval));
            GCStats.total_allocations++;
            GCStats.total_bytes_requested += (ulong)sizeof(stackval);
            val_heap.Enqueue((nint)p, 0);
            return p;
        }
        public static stackval* AllocValue(WaveClass @class)
        {
            if (!@class.IsPrimitive)
                return null;
            var p = AllocValue();
            p->type = @class.TypeCode;
            return p;
        }

        public static IshtarObject* AllocString(string str, IshtarObject** node = null)
        {
            var arg = AllocObject(WaveTypeCode.TYPE_STRING.AsRuntimeClass(), node);
            var clazz = IshtarUnsafe.AsRef<RuntimeIshtarClass>(arg->clazz);
            arg->vtable[clazz.Field["!!value"].vtable_offset] = StringStorage.Intern(str);
            return arg;
        }




        public static IshtarObject* AllocObject(RuntimeIshtarClass @class, IshtarObject** node = null)
        {
            var p = (IshtarObject*) Marshal.AllocHGlobal(sizeof(IshtarObject));

            Unsafe.InitBlock(p, 0, (uint)sizeof(IshtarObject));
            
            heap.Enqueue((nint)p, 0);

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