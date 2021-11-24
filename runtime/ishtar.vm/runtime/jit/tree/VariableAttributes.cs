namespace ishtar.jit;

using registers;

internal class VariableAttributes
{
    public VariableData VariableData { get; set; }

    public VariableFlags Flags { get; set; }

    public int UsageCount { get; set; }

    public int InRegIndex { get; set; }

    public int OutRegIndex { get; set; }

    public int InRegs { get; set; }

    public int AllocableRegs { get; set; }

    public void Setup(VariableData vd, VariableFlags flags = 0, int inRegs = 0, int allocableRegs = 0)
    {
        VariableData = vd;
        Flags = flags;
        UsageCount = 0;
        InRegIndex = REGISTER_INDEX.INVALID;
        OutRegIndex = REGISTER_INDEX.INVALID;
        InRegs = inRegs;
        AllocableRegs = allocableRegs;
    }

    public void CopyFrom(VariableAttributes va)
    {
        VariableData = va.VariableData;
        Flags = va.Flags;
        UsageCount = va.UsageCount;
        InRegIndex = va.InRegIndex;
        OutRegIndex = va.OutRegIndex;
        InRegs = va.InRegs;
        AllocableRegs = va.AllocableRegs;
    }
}