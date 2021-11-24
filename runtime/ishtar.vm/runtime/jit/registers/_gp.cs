namespace ishtar.jit.registers;

public class _gp : _reg
{
    private static Dictionary<GP_REGISTER_TYPE, int> _sizeMap = new Dictionary<GP_REGISTER_TYPE, int>
    {
        {GP_REGISTER_TYPE.GpbLo, 1},
        {GP_REGISTER_TYPE.GpbHi, 1},
        {GP_REGISTER_TYPE.Gpw, 2},
        {GP_REGISTER_TYPE.Gpd, 4},
        {GP_REGISTER_TYPE.Gpq, 8}
    };  

    internal _gp(_gp other) : base(other)  { }

    internal _gp(GP_REGISTER_TYPE type, int index)
    {
        REG_TYPE = (REGISTER_TYPE) type;
        INDEX = index;
        SIZE = _sizeMap[type];
    }
}
