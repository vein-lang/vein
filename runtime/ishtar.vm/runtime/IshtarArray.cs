namespace ishtar
{
    using System;
    using System.Runtime.CompilerServices;

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
        public RuntimeIshtarClass ElementClass
            => element_clazz is null ? null : IshtarUnsafe.AsRef<RuntimeIshtarClass>(element_clazz);

        public void* clazz => memory->clazz;
        public void* element_clazz;

        public IshtarObject* memory;
        public Block _block;

        public IshtarObject* this[uint index, CallFrame frame]
        {
            get => Get(index, frame);
            set => Set(index, value, frame);
        }

        public IshtarObject* Get(uint index, CallFrame frame = null)
        {
            if (!ElementClass.IsPrimitive) return elements[index];
            var result = IshtarGC.AllocObject(ElementClass);
            var offset = result->DecodeClass().Field["!!value"].vtable_offset;
            result->vtable[offset] = elements[index]->vtable[offset];
            return result;
        }

        public void Set(uint index, IshtarObject* value, CallFrame frame = null)
        {
            FFI.StaticValidate(frame, &value);
            var value_class = value->DecodeClass();
            VM.Assert(value_class.TypeCode == ElementClass.TypeCode, WNE.TYPE_MISMATCH, $"", frame);
            if (index > length)
            {
                VM.FastFail(WNE.OUT_OF_RANGE, $"", frame);
                VM.ValidateLastError();
                return;
            }

            if (Class.IsPrimitive)
            {
                var offset = value_class.Field["!!value"].vtable_offset;
                elements[index]->vtable[offset] = value->vtable[offset];
            }
            else
                elements[index] = value;
        }


        public struct Block : IEquatable<Block>
        {
            public uint offset_value;
            public uint offset_block;
            public uint offset_rank;
            public uint offset_size;

            #region IEquatable<Block>

            public bool Equals(Block other) => offset_value == other.offset_value && offset_block == other.offset_block && offset_rank == other.offset_rank && offset_size == other.offset_size;

            public override bool Equals(object obj) => obj is Block other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(offset_value, offset_block, offset_rank, offset_size);

            #endregion
        }

        #region IEquatable<IshtarArray>

        public bool Equals(IshtarArray other) => memory == other.memory && _block.Equals(other._block);

        public override bool Equals(object obj) => obj is IshtarArray other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(unchecked((int)(long)memory), _block);

        #endregion
    }
}
