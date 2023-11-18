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
            if (size == 0)
                throw new ArgumentException($"size is not allowed zero");

            static stackval* allocArray(CallFrame frame, int size)
                => frame.vm.GC.AllocateStack(frame, size);
            static void freeArray(CallFrame frame, stackval* stack, int size)
                => frame.vm.GC.FreeStack(frame, stack, size);

            static stackval* alloc(CallFrame frame, int size)
                => frame.vm.GC.AllocValue();
            static void free(CallFrame frame, stackval* stack, int size)
                => frame.vm.GC.FreeValue(stack);


            var p = new SmartPointer<stackval>(size, frame,
                size == 1 ? &alloc : &allocArray,
                size == 1 ? &free : &freeArray);
#if DEBUG
            p.CaptureAddress();
#endif
            return p;
        }
    }
}
