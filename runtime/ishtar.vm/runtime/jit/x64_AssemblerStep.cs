namespace ishtar;

using Iced.Intel;

public struct x64_AssemblerStep
{
    public InstructionTarget Instruction;
    public Register Register;
    public int StackOffset;

    public enum InstructionTarget
    {
        push,
        mov
    }
}