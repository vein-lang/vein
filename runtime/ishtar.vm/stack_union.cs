namespace ishtar
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct stack_union
    {
        [FieldOffset(0)] public sbyte b;
        [FieldOffset(0)] public short s;
        [FieldOffset(0)] public int i;
        [FieldOffset(0)] public long l;
        [FieldOffset(0)] public byte ub;
        [FieldOffset(0)] public ushort us;
        [FieldOffset(0)] public uint ui;
        [FieldOffset(0)] public ulong ul;
        [FieldOffset(0)] public float f_r4;
        [FieldOffset(0)] public double f;
        [FieldOffset(0)] public decimal d;
        [FieldOffset(0)] public Half hf;
        [FieldOffset(0)] public IntPtr p;
    }
}
