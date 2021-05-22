namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
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

        public static IshtarObject* WrapValue(CallFrame frame, stackval* p, IshtarObject** node = null)
        {
            if (p->type is TYPE_OBJECT or TYPE_CLASS or TYPE_STRING or TYPE_ARRAY)
                return (IshtarObject*)p->data.p;
            if (p->type == TYPE_NONE || p->type > TYPE_ARRAY || p->type < TYPE_NONE)
            {
                VM.FastFail(WNE.ACCESS_VIOLATION, 
                    "Scalar value type cannot be extracted.\n" +
                    "Invalid memory address is possible.\n" +
                    "Please report the problem into https://github.com/0xF6/mana_lang/issues.", 
                    frame);
                VM.ValidateLastError();
            }

            var clazz = p->type.AsRuntimeClass();
            var obj = AllocObject(clazz, node);

            FFI.StaticValidateField(frame, &obj, "!!value");

            obj->vtable[clazz.Field["!!value"].vtable_offset] = p->type switch
            {
                TYPE_I1 => (sbyte*) p->data.b, TYPE_U1 => (byte  *) p->data.ub,
                TYPE_I2 => (short*) p->data.s, TYPE_U2 => (ushort*) p->data.us,
                TYPE_I4 => (int  *) p->data.i, TYPE_U4 => (uint  *) p->data.ui,
                TYPE_I8 => (long *) p->data.l, TYPE_U8 => (ulong *) p->data.ul,
                
                TYPE_BOOLEAN => (int  *) p->data.i,
                TYPE_CHAR    => (int  *) p->data.i,

                _ => &*p
            };

            return obj;
        }
        
        public static IshtarObject* AllocString(string str, IshtarObject** node = null)
        {
            var arg = AllocObject(TYPE_STRING.AsRuntimeClass(), node);
            var clazz = IshtarUnsafe.AsRef<RuntimeIshtarClass>(arg->clazz);
            arg->vtable[clazz.Field["!!value"].vtable_offset] = StringStorage.Intern(str);
            return arg;
        }

        public static IshtarObject* AllocInt(int value, IshtarObject** node = null)
        {
            var obj = AllocObject(TYPE_I4.AsRuntimeClass(), node);
            var clazz = IshtarUnsafe.AsRef<RuntimeIshtarClass>(obj->clazz);
            obj->vtable[clazz.Field["!!value"].vtable_offset] = (int*)value;

            GCStats.alive_objects++;
            GCStats.total_allocations++;
            GCStats.total_bytes_requested += (ulong)sizeof(IshtarObject);

            return obj;
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