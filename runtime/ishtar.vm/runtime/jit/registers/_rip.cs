namespace ishtar.jit.registers;

public class _rip : _reg
{
    internal _rip()
    {
        this.REG_TYPE = REGISTER_TYPE.RIP;
        this.INDEX = 0;
        this.SIZE = 0;
    }
}
