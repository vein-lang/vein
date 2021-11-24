namespace ishtar.jit.registers;

public sealed class INVALID_OPERAND : _operand
{
    internal INVALID_OPERAND() : base(OPERAND_TYPE.INVALID) { }
}
