namespace ishtar
{
    using System;

    public unsafe struct IshtarArray : IEquatable<IshtarArray>
    {
        public const uint MAX_SIZE = 0xFFFFFFFFU;

        public ulong rank
            => (ulong)(long*)memory->vtable[_block.offset_rank];
        public ulong length
            => (ulong)(long*)memory->vtable[_block.offset_size];
        public ulong block_size
            => (ulong)(long*)memory->vtable[_block.offset_block];
        public IshtarObject** elements
            => (IshtarObject**)memory->vtable[_block.offset_value];
        public RuntimeIshtarClass Class
            => clazz is null ? null : IshtarUnsafe.AsRef<RuntimeIshtarClass>(clazz);

        public void* clazz => memory->clazz;

        public IshtarObject* memory;
        public Block _block;


        public struct Block : IEquatable<Block>
        {
            public int offset_value;
            public int offset_block;
            public int offset_rank;
            public int offset_size;

            #region IEquatable<Block>

            public bool Equals(Block other) => offset_value == other.offset_value && offset_block == other.offset_block && offset_rank == other.offset_rank && offset_size == other.offset_size;

            public override bool Equals(object obj) => obj is Block other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(offset_value, offset_block, offset_rank, offset_size);

            #endregion
        }

        #region IEquatable<IshtarArray>

        public bool Equals(IshtarArray other) => memory == other.memory && _block.Equals(other._block);

        public override bool Equals(object obj) => obj is IshtarArray other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(unchecked((int) (long) memory), _block);

        #endregion
    }
}
