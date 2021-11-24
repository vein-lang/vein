namespace ishtar.jit.registers;

public class _mm : _reg
{
    internal _mm(int index)
    {
        REG_TYPE = REGISTER_TYPE.MM;
        INDEX = index;
        SIZE = 8;
    }

    internal _mm(_mm other) : base(other) { }
}
