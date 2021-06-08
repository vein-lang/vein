namespace ishtar
{
    using System;

    public unsafe struct IshtarArray
    {
        public int rank;
        public int len;
        public IshtarObject** elements;

        public void* clazz;


        public RuntimeIshtarClass DecodeClass()
        {
            if (clazz is null)
                return null;
            return IshtarUnsafe.AsRef<RuntimeIshtarClass>(clazz);
        }
    }
}
