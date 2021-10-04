namespace ishtar
{
    using System;
    using mana.runtime;

    public struct stackval
    {
        public stack_union data;
        public ManaTypeCode type;

        public static stackval[] Alloc(int size) => GC.AllocateArray<stackval>(size, true);


        public void validate(CallFrame frame, ManaTypeCode typeCode) =>
            VM.Assert(type == ManaTypeCode.TYPE_ARRAY, WNE.TYPE_MISMATCH,
                $"stack type mismatch, current: '{type}', expected: '{typeCode}'. opcode: '{frame.last_ip}'", frame);
    }
}
