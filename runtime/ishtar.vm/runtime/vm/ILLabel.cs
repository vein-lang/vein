namespace ishtar
{
    public readonly struct ILLabel(int pos, OpCodeValue opcode)
    {
        public int pos { get; } = pos;
        public OpCodeValue opcode { get; } = opcode;
    }
}
