namespace ishtar
{
    public unsafe struct PInvokeInfo
    {
        public ushort iflags;
        public void* Addr;

        public static PInvokeInfo New(void* p) => new() { Addr = p };
    }
}
