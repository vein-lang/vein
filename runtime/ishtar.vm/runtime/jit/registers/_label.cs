namespace ishtar.jit.registers;


public class _label : _operand
{
    internal _label(int id) : base(OPERAND_TYPE.LABEL) =>
        ID = id;

    internal void Reset()
    {
        ID = _constants.INVALID_ID;
        SIZE = 0;
    }
}

