namespace ishtar;


[CTypeExport("ishtar_illabel_t")]
public readonly struct ILLabel(int pos, OpCodeValue opcode) : IEquatable<ILLabel>
{
    public int pos { get; } = pos;
    public OpCodeValue opcode { get; } = opcode;

    public bool Equals(ILLabel other) => pos == other.pos && opcode == other.opcode;

    public override bool Equals(object obj) => obj is ILLabel other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(pos, (int)opcode);
}
