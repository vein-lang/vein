namespace ishtar.jit.registers;

public class _xmm : _reg
{
    internal _xmm(int index)
    {
        REG_TYPE = REGISTER_TYPE.XMM;
        INDEX = index;
        SIZE = 16;
    }

    internal _xmm(_xmm other) : base(other)  { }
}
