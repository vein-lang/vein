namespace ishtar
{
    using System.Runtime.InteropServices;
    using vein.runtime;

    public unsafe struct IshtarObject
    {
        public void* clazz;
        public void** vtable;
        public GCFlags flags;

        public uint vtable_size;

        public IshtarObject** owner;
#if DEBUG
        public long __gc_id = -1;
#endif

        public RuntimeIshtarClass decodeClass()
        {
            if (clazz is null)
                return null;
            return IshtarUnsafe.AsRef<RuntimeIshtarClass>(clazz);
        }

        public static IshtarObject* IsInstanceOf(CallFrame frame, IshtarObject* @this, RuntimeIshtarClass @class)
        {
            if (!@class.is_inited)
                @class.init_vtable();
            if (@this == null)
                return null;
            if (@class.IsInterface)
                return IsInstanceOfByRef(frame, @this, @class);
            return IsAssignableFrom(frame, @class, @this->decodeClass()) ? @this : null;
        }

        public static IshtarObject* IsInstanceOfByRef(CallFrame frame, IshtarObject* c, RuntimeIshtarClass @class)
        {
            // temporary cast to\from interface is not support
            frame.ThrowException(KnowTypes.IncorrectCastFault(frame));
            return c;
        }

        public static bool IsAssignableFrom(CallFrame frame, RuntimeIshtarClass c1, RuntimeIshtarClass c2)
        {
            if (!c1.is_inited) c1.init_vtable();
            if (!c2.is_inited) c2.init_vtable();
            // TODO: Array detection
            // TODO: Generic detection
            // TODO: Interfrace detection
            if (c1.FullName == VeinCore.ObjectClass.FullName)
                return true;
            return c1.IsInner(c2);
        }
    }


    public abstract unsafe class NIObject
    {
        protected readonly IshtarObject* __value__;
        protected NIObject(IshtarObject* obj)
        {
            this.__value__ = obj;
            this.__value__->flags |= GCFlags.IMMORTAL;
        }

        public abstract RuntimeIshtarClass Type { get; }
    }
}
