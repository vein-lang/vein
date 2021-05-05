namespace ishtar
{
    public unsafe class CallFrameException
    {
        public uint* last_ip;
        public IshtarObject* value;
        public string stack_trace;
    };
}