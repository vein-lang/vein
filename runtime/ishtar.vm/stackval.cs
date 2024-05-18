namespace ishtar
{
    using collections;
    using System.Runtime.InteropServices;
    using vein.runtime;

    public struct stackval : IEq<stackval>, IDirectEq<stackval>, IEquatable<stackval>
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
                => frame.vm.GC.AllocValue(frame);
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

        public static unsafe bool Eq(stackval* p1, stackval* p2)
        {
            if (p1->type != p2->type)
                return false;

            var s1 = AsSpan(ref *p1);
            var s2 = AsSpan(ref *p2);

            return s1.SequenceEqual(s2);
        }

        private static Span<byte> AsSpan(ref stackval val)
        {
            Span<stackval> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<stackval, byte>(valSpan);
        }

        public static bool Eq(ref stackval p1, ref stackval p2)
        {
            if (p1.type != p2.type)
                return false;

            var s1 = AsSpan(ref p1);
            var s2 = AsSpan(ref p2);

            return s1.SequenceEqual(s2);
        }

        public bool Equals(stackval other) => Eq(ref this, ref other);


        public override bool Equals(object obj) => obj is stackval other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(data.d, (int)type);
    }
}
