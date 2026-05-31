namespace vein.runtime
{
    using lang.c;

    [CTypeExport("struct_layout_t")]
    [CEnumPrefix("LAYOUT_")]
    public enum VeinStructLayoutKind : byte
    {
        Auto = 0,
        Sequential = 1,
        Explicit = 2,
    }
}
