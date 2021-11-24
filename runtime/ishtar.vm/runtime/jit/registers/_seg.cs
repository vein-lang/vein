namespace ishtar.jit.registers;

public class _seg : _reg
{
    internal _seg(int index)
    {
        REG_TYPE = REGISTER_TYPE.SEG;
        INDEX = index;
        SIZE = 2;
    }

    internal _seg(_seg other) : base(other) { }
}
