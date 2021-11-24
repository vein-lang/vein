namespace ishtar.jit.registers;

public abstract class _operand
{
    protected _operand(OPERAND_TYPE type)
    {
        ID = _constants.INVALID_ID;
        OP_TYPE = type;
    }

    protected _operand(OPERAND_TYPE type, int size)
    {
        ID = _constants.INVALID_ID;
        OP_TYPE = type;
        SIZE = size;
    }

    protected _operand(_operand other)
    {
        OP_TYPE = other.OP_TYPE;
        ID = other.ID;
        Reserved0 = other.Reserved0;
        Reserved1 = other.Reserved1;
        SIZE = other.SIZE;
        Reserved2 = other.Reserved2;
        Reserved3 = other.Reserved3;
    }

    protected internal OPERAND_TYPE OP_TYPE { get; protected set; }

    protected internal int SIZE { get; protected set; }

    protected internal int Reserved0 { get; protected set; }

    protected internal int Reserved1 { get; protected set; }

    protected internal int ID { get; protected set; }

    protected internal int Reserved2 { get; protected set; }

    protected internal int Reserved3 { get; protected set; }

    public static INVALID_OPERAND INVALID => new INVALID_OPERAND();

    internal virtual T As<T>() where T : _operand => this as T;

    public override string ToString() =>
        $"[{OP_TYPE}: Id={(ID == _constants.INVALID_ID ? "#" : ID.ToString())}, Size={SIZE}]";
}
