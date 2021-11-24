namespace ishtar.jit;

using registers;

internal class VariableData
{
    public VariableData(VariableType type, VariableInfo info, int id, string name = null, int alignment = 0)
    {
        Type = type;
        Info = info;
        Id = id;
        Name = name;
        LocalId = _constants.INVALID_ID;
        RegisterIndex = REGISTER_INDEX.INVALID;
        State = VariableUsage.None;
        Priority = 10;
        Alignment = alignment == 0 ? Math.Min(info.Size, 64) : Math.Min(alignment, 64);
        if (type != VariableType.Stack) return;
        IsStack = true;
        Type = VariableType.Invalid;
    }

    public VariableType Type { get; private set; }

    public VariableInfo Info { get; private set; }

    public int Id { get; private set; }

    public int LocalId { get; set; }

    public int RegisterIndex { get; set; }

    public int Priority { get; set; }

    public string Name { get; private set; }

    public VariableAttributes Attributes { get; set; }

    public bool IsMemoryArgument { get; set; }

    public bool IsCalculated { get; private set; }

    public bool IsModified { get; set; }

    public bool SaveOnUnuse { get; private set; }

    public bool IsStack { get; private set; }

    public int Alignment { get; private set; }

    public VariableUsage State { get; internal set; }

    public VariableCell MemoryCell { get; internal set; }

    public int HomeMask { get; set; }

    public int MemoryOffset { get; set; }

    public override string ToString()
        => $"{(string.IsNullOrEmpty(Name) ? "NA" : Name)} [{Type}]:{RegisterIndex}";
}

internal class VariableInfo
{
    public VariableInfo(REGISTER_TYPE registerType, int size, RegisterClass registerClass, VariableValueFlags valueFlags, string name)
    {
        RegisterType = registerType;
        Size = size;
        RegisterClass = registerClass;
        ValueFlags = valueFlags;
        Name = name;
    }

    public REGISTER_TYPE RegisterType { get; private set; }

    public int Size { get; private set; }

    public RegisterClass RegisterClass { get; private set; }

    public VariableValueFlags ValueFlags { get; private set; }

    public string Name { get; private set; }
}


[Flags]
public enum VariableValueFlags
{
    Sp = 0x10,
    Dp = 0x20,
    Packed = 0x40
}
