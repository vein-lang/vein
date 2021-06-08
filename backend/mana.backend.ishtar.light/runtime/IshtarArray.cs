namespace ishtar
{
    using System;

    public unsafe struct IshtarArray : IEquatable<IshtarArray>
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

        #region IEquatable<IshtarArray>

        public bool Equals(IshtarArray other)
            => rank == other.rank &&
               len == other.len &&
               elements == other.elements &&
               clazz == other.clazz;

        public override bool Equals(object obj)
            => obj is IshtarArray other &&
               Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(rank, len, unchecked((int) (long) elements), unchecked((int) (long) clazz));

        #endregion
    }
}
