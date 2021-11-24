namespace ishtar.jit;

internal enum CodeNodeType
{
    None = 0,
    Instruction,
    Data,
    Alignment,
    Label,
    Comment,
    Sentinel,
    Hint,
    Function,
    Return,
    Call,
    CallArgument
}
