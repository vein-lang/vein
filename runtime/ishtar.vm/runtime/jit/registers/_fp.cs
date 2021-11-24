namespace ishtar.jit.registers;

public class _fp : _reg
{
    internal _fp(int index)
    {
        REG_TYPE = REGISTER_TYPE.FP;
        INDEX = index;
        SIZE = 10;
    }

    internal _fp(_fp other)  : base(other) { }
}
