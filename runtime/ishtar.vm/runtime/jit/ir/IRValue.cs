namespace ishtar.jit;

using ishtar.collections;
using vein.runtime;

/// <summary>
/// IR type system — maps Vein types to JIT-level types.
/// </summary>
public enum IRType : byte
{
    Void,
    I1,
    I2,
    I4,
    I8,
    U1,
    U2,
    U4,
    U8,
    R4,
    R8,
    Ptr,
    Bool,
}

public static class IRTypeMap
{
    public static IRType FromVein(VeinTypeCode code) => code switch
    {
        VeinTypeCode.TYPE_BOOLEAN => IRType.Bool,
        VeinTypeCode.TYPE_I1 => IRType.I1,
        VeinTypeCode.TYPE_I2 => IRType.I2,
        VeinTypeCode.TYPE_I4 => IRType.I4,
        VeinTypeCode.TYPE_I8 => IRType.I8,
        VeinTypeCode.TYPE_U1 => IRType.U1,
        VeinTypeCode.TYPE_U2 => IRType.U2,
        VeinTypeCode.TYPE_U4 => IRType.U4,
        VeinTypeCode.TYPE_U8 => IRType.U8,
        VeinTypeCode.TYPE_R4 => IRType.R4,
        VeinTypeCode.TYPE_R8 => IRType.R8,
        VeinTypeCode.TYPE_STRING => IRType.Ptr,
        VeinTypeCode.TYPE_OBJECT => IRType.Ptr,
        VeinTypeCode.TYPE_ARRAY => IRType.Ptr,
        VeinTypeCode.TYPE_CLASS => IRType.Ptr,
        VeinTypeCode.TYPE_VOID => IRType.Void,
        _ => IRType.Ptr
    };

    public static int SizeOf(IRType type) => type switch
    {
        IRType.I1 or IRType.U1 or IRType.Bool => 1,
        IRType.I2 or IRType.U2 => 2,
        IRType.I4 or IRType.U4 or IRType.R4 => 4,
        IRType.I8 or IRType.U8 or IRType.R8 or IRType.Ptr => 8,
        _ => 0
    };

    public static bool IsInteger(IRType type) => type is IRType.I1 or IRType.I2 or IRType.I4 or IRType.I8
        or IRType.U1 or IRType.U2 or IRType.U4 or IRType.U8 or IRType.Bool;

    public static bool IsFloat(IRType type) => type is IRType.R4 or IRType.R8;
}

/// <summary>
/// SSA-form IR value handle. Unmanaged struct.
/// Every computation produces a numbered value stored in the IRFunction's value table.
/// </summary>
public unsafe struct IRValue : IEq<IRValue>, IEquatable<IRValue>
{
    public int Id;
    public IRType Type;
    /// <summary>Index of the defining instruction in its block, or -1 for block args/phi.</summary>
    public int DefInstrIndex;
    /// <summary>Index of the basic block that defines this value.</summary>
    public int DefBlockIndex;

    public static bool Eq(IRValue* p1, IRValue* p2) => p1->Id == p2->Id;
    public bool Equals(IRValue other) => Id == other.Id;
}
