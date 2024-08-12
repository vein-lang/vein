namespace ishtar
{
    using collections;
    using vein.runtime;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct rawval_union
    {
        [FieldOffset(0)] public RuntimeIshtarMethod* m;
        [FieldOffset(0)] public RuntimeIshtarClass* c;
    }

    public enum VeinRawCode
    {
        ISHTAR_ERROR,
        ISHTAR_METHOD,
        ISHTAR_CLASS
    }

    public unsafe struct rawval
    {
        public rawval_union data;
        public VeinRawCode type;

        public static unsafe SmartPointer<rawval> Allocate(CallFrame* frame, short size)
            => Allocate(frame, (ushort)size);

        public static unsafe SmartPointer<rawval> Allocate(CallFrame* frame, ushort size)
        {
            if (size == 0)
                throw new ArgumentException($"size is not allowed zero");

            static rawval* allocArray(CallFrame* frame, int size)
                => throw new NotSupportedException();
            static void freeArray(CallFrame* frame, rawval* stack, int size)
                => throw new NotSupportedException();

            static rawval* alloc(CallFrame* frame, int size)
                => frame->vm->gc->AllocRawValue(frame);
            static void free(CallFrame* frame, rawval* stack, int size)
                => frame->vm->gc->FreeRawValue(stack);


            var p = new SmartPointer<rawval>(size, frame,
                size == 1 ? &alloc : &allocArray,
                size == 1 ? &free : &freeArray);

            return p;
        }
    }


    [DebuggerDisplay("{ToString()}")]
    public unsafe struct stackval : IEq<stackval>, IDirectEq<stackval>, IEquatable<stackval>
    {
        public stack_union data;
        public VeinTypeCode type;
        
        public void validate(CallFrame* frame, VeinTypeCode typeCode) =>
            VirtualMachine.Assert(type == VeinTypeCode.TYPE_ARRAY, WNE.TYPE_MISMATCH,
                $"stack type mismatch, current: '{type}', expected: '{typeCode}'. opcode: '{frame->last_ip}'", frame);


        public static unsafe SmartPointer<stackval> Allocate(CallFrame* frame, short size)
            => Allocate(frame, (ushort)size);

        public static unsafe SmartPointer<stackval> Allocate(CallFrame* frame, ushort size)
        {
            if (size == 0)
                throw new ArgumentException($"size is not allowed zero");

            static stackval* allocArray(CallFrame* frame, int size)
                => frame->vm->gc->AllocateStack(frame, size);
            static void freeArray(CallFrame* frame, stackval* stack, int size)
                => frame->vm->gc->FreeStack(frame, stack, size);

            static stackval* alloc(CallFrame* frame, int size)
                => frame->vm->gc->AllocValue(frame);
            static void free(CallFrame* frame, stackval* stack, int size)
                => frame->vm->gc->FreeValue(stack);


            var p = new SmartPointer<stackval>(size, frame,
                size == 1 ? &alloc : &allocArray,
                size == 1 ? &free : &freeArray);

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

        public override string ToString() =>
            type switch
            {
                VeinTypeCode.TYPE_NONE => $"[type {type}]",
                VeinTypeCode.TYPE_VOID => $"[type {type}]",
                VeinTypeCode.TYPE_CLASS => $"[type {type}]",
                VeinTypeCode.TYPE_ARRAY => $"[type {type}]",
                VeinTypeCode.TYPE_FUNCTION => $"[type {type}]",
                VeinTypeCode.TYPE_TOKEN => $"[type {type}]",
                VeinTypeCode.TYPE_STRING => $"[type {type}]",
                VeinTypeCode.TYPE_OBJECT => $"[type {type}]",
                VeinTypeCode.TYPE_BOOLEAN => $"[type {type} - {data.i == 1}]",
                VeinTypeCode.TYPE_CHAR => $"[type {type} - {data.i}]",
                VeinTypeCode.TYPE_I4 => $"[type {type} - {data.i}]",
                VeinTypeCode.TYPE_I1 => $"[type {type} - {data.b}]",
                VeinTypeCode.TYPE_U1 => $"[type {type} - {data.ub}]",
                VeinTypeCode.TYPE_I2 => $"[type {type} - {data.s}]",
                VeinTypeCode.TYPE_U2 => $"[type {type} - {data.us}]",
                VeinTypeCode.TYPE_U4 => $"[type {type} - {data.ui}]",
                VeinTypeCode.TYPE_I8 => $"[type {type} - {data.l}]",
                VeinTypeCode.TYPE_U8 => $"[type {type} - {data.ul}]",
                VeinTypeCode.TYPE_R2 => $"[type {type} - {data.hf}]",
                VeinTypeCode.TYPE_R4 => $"[type {type} - {data.f_r4}]",
                VeinTypeCode.TYPE_R8 => $"[type {type} - {data.f}]",
                VeinTypeCode.TYPE_R16 => $"[type {type} - {data.d}]",
                VeinTypeCode.TYPE_RAW => $"[type {type} - {data.d}]",
                _ => $"[type !!BAD!!]"
            };
    }
}
