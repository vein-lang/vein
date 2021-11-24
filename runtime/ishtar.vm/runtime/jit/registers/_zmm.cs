namespace ishtar.jit.registers;

public class _zmm : _reg
{
    internal _zmm(int index)
    {
        REG_TYPE = REGISTER_TYPE.ZMM;
        INDEX = index;
        SIZE = 64;
    }

    internal _zmm(_zmm other) : base(other) { }
}
