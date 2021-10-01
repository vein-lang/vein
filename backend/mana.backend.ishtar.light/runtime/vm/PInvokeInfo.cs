namespace ishtar
{
    public unsafe struct PInvokeInfo
    {
        public ushort iflags;
        public void* Addr;

        public static PInvokeInfo New(void* p) => new() { Addr = p };


        public static implicit operator void*(PInvokeInfo p) => p.Addr;
        public static implicit operator PInvokeInfo(void* p) => New(p);
    }
}
