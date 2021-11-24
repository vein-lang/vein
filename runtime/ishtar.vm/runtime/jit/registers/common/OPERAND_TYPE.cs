namespace ishtar.jit.registers;

public enum OPERAND_TYPE
{
    INVALID = 0b0,
    REGISTER = 0b1,
    VARIABLE = 0b10,
    MEMORY = 0b11,
    IMMEDIATE = 0b100,
    LABEL = 0b101
}
