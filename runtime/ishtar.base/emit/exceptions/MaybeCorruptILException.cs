namespace mana.exceptions
{
    using System;
    using ishtar.emit;

    public class MaybeCorruptILException : Exception
    {
        public MaybeCorruptILException(int size, int new_size, OpCode opcode) :
            base($"Emit '{opcode.Name}' resulted in an invalid buffer size value. amount: {new_size - size}, excepted: {opcode.Size}")
        {

        }
    }
}
