namespace ishtar
{
    using System;

    public class jit_x86
    {
        public static int EAX = 0;
        public static int EСX = 1;
        public static int EDX = 2;
        public static int EBX = 3;
        public static int ESP = 4;
        public static int EBP = 5;
        public static int ESI = 6;
        public static int EDI = 7;
    }

    public unsafe struct jit_buffer
    {
        public void* p;
        public ushort* b;
        public uint* w;
        public int* c;
        public int* i;
    }

    public unsafe struct _jlist
    {
        public int pos, target;
        public _jlist* next;
    }
}
