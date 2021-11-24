namespace ishtar.jit.registers;

public abstract class _reg : _operand
{
    protected _reg()
        : base(OPERAND_TYPE.REGISTER)
    {
        this.REG_TYPE = REGISTER_TYPE.INVALID;
        this.INDEX = REGISTER_INDEX.INVALID;
    }

    protected _reg(_reg other)
        : base(other)
    {
    }

    internal int INDEX
    {
        get => Reserved0;
        set => Reserved0 = value;
    }

    internal REGISTER_TYPE REG_TYPE
    {
        get => (REGISTER_TYPE)Reserved1;
        set => Reserved1 = (int)value;
    }

    //internal static _reg FromVariable(Variable var, int index)
    //{
    //    switch (var.RegisterType)
    //    {
    //        case REGISTER_TYPE.GpbLo:
    //            return new GpRegister(GP_REGISTER_TYPE.GpbLo, index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.GpbHi:
    //            return new GpRegister(GP_REGISTER_TYPE.GpbHi, index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.Gpw:
    //            return new GpRegister(GP_REGISTER_TYPE.Gpw, index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.Gpd:
    //            return new GpRegister(GP_REGISTER_TYPE.Gpd, index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.GPQ:
    //            return new GpRegister(GP_REGISTER_TYPE.Gpq, index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.FP:
    //            return new FpRegister(index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.MM:
    //            return new MmRegister(index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.K:
    //            return new KRegister(index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.XMM:
    //            return new XmmRegister(index) {Id = var.Id, Size = var.Size};
    //        case REGISTER_TYPE.YMM:
    //            return new YmmRegister(index) { Id = var.Id, Size = var.Size };
    //        case REGISTER_TYPE.ZMM:
    //            return new ZmmRegister(index) { Id = var.Id, Size = var.Size };
    //        default:
    //            throw new ArgumentOutOfRangeException();
    //    }
    //}

    public override string ToString() =>
        $"[{OP_TYPE}: Id={(ID == _constants.INVALID_ID ? "#" : ID.ToString())}," +
        $" Size={SIZE}, Type={REG_TYPE}, Idx={(INDEX == REGISTER_INDEX.INVALID ? "#" : INDEX.ToString())}]";
}
