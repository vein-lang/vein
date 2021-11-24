namespace ishtar.jit.registers;

public class _ymm : _reg
{
    internal _ymm(int index)
    {
        REG_TYPE = REGISTER_TYPE.YMM;
        INDEX = index;
        SIZE = 32;
    }

    internal _ymm(_ymm other) : base(other)
    {
    }
}
