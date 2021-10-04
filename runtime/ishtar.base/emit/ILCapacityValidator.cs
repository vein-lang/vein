namespace mana.ishtar.emit
{
    using System;
    using System.Runtime.CompilerServices;
    using exceptions;

    internal unsafe class ILCapacityValidator : IDisposable
    {
        private readonly OpCode _opcode;
        private readonly int* _size;
        private readonly int _initial_size;

        private ILCapacityValidator(ref int size, OpCode opcode)
        {
            _opcode = opcode;
            _size = (int*)Unsafe.AsPointer(ref size);
            _initial_size = size;
        }

        public static IDisposable Begin(ref int size, OpCode opcode)
            => new ILCapacityValidator(ref size, opcode);

        void IDisposable.Dispose()
        {
            ref int i = ref *_size;
            if (Math.Abs(_initial_size - (i - sizeof(ushort))) != _opcode.Size)
                throw new MaybeCorruptILException(_initial_size, (i - sizeof(ushort)), _opcode);
        }
    }
}
