namespace ishtar
{
    using vein.runtime;

    public struct stackval
    {
        public stack_union data;
        public VeinTypeCode type;
        
        public void validate(CallFrame frame, VeinTypeCode typeCode) =>
            VirtualMachine.Assert(type == VeinTypeCode.TYPE_ARRAY, WNE.TYPE_MISMATCH,
                $"stack type mismatch, current: '{type}', expected: '{typeCode}'. opcode: '{frame.last_ip}'", frame);


        public static unsafe SmartPointer<stackval> Allocate(CallFrame frame, short size)
            => Allocate(frame, (ushort)size);

        public static unsafe SmartPointer<stackval> Allocate(CallFrame frame, ushort size)
        {
            static stackval* alloc(CallFrame frame, int size)
                => frame.vm.GC.AllocateStack(frame, size);
            static void free(CallFrame frame, stackval* stack, int size)
                => frame.vm.GC.FreeStack(frame, stack, size);

            return new SmartPointer<stackval>(size, frame, &alloc, &free);
        }
    }
}
