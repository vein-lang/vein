namespace ishtar;

using Iced.Intel;

[CTypeExport("x64_asm_step_t")]
public struct x64_AssemblerStep
{
    public InstructionTarget Instruction;
    public Register Register;
    public int StackOffset;

    [CTypeExport("x64_instruction_target_t")]
    public enum InstructionTarget
    {
        push,
        mov
    }
}
