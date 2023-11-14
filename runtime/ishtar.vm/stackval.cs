namespace ishtar
{
    using System;
    using vein.runtime;

    public struct stackval
    {
        public stack_union data;
        public VeinTypeCode type;

        public static stackval[] Alloc(int size) => GC.AllocateArray<stackval>(size, true);


        public void validate(CallFrame frame, VeinTypeCode typeCode) =>
            VirtualMachine.Assert(type == VeinTypeCode.TYPE_ARRAY, WNE.TYPE_MISMATCH,
                $"stack type mismatch, current: '{type}', expected: '{typeCode}'. opcode: '{frame.last_ip}'", frame);
    }
}
