namespace ishtar.jit.registers;

public class _k : _reg
{
    internal _k(int index)
    {
        REG_TYPE = REGISTER_TYPE.K;
        INDEX = index;
        SIZE = 8;
    }

    internal _k(_k other) : base(other) { }
}
