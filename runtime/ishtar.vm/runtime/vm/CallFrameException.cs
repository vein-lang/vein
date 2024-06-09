namespace ishtar
{
    public unsafe struct CallFrameException
    {
        public bool IsDefault() =>
            last_ip is null &&
            value is null &&
            stack_trace is null;

        public uint* last_ip;
        public IshtarObject* value;
        public InternedString* stack_trace;

        public string GetStackTrace()
        {
            if (stack_trace is null)
                return "";
            return StringStorage.GetStringUnsafe(stack_trace);
        }
    };
}
