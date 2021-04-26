namespace ishtar
{
    public unsafe class CallFrameException
    {
        public uint* last_ip;
        public WaveObject value;
        public string stack_trace;
    };
}