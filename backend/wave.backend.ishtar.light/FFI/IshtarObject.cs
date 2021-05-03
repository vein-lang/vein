namespace ishtar
{
    using System.Runtime.CompilerServices;

    public unsafe struct IshtarObject
    {
        public void* clazz;
        public void** vtable;
        public GCFlags flags;

        public uint vtable_size;

        public IshtarObject** owner;

        public RuntimeIshtarClass Unpack()
        {
            if (clazz is null)
                return null;
            return IshtarUnsafe.AsRef<RuntimeIshtarClass>(clazz);
        }
    }
}