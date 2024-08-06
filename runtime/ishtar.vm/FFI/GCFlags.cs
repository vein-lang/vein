namespace ishtar
{
    [Flags]
    [CTypeExport("gc_flags_t")]
    public enum GCFlags
    {
        NONE = 0,
        NATIVE_REF = 1 << 1,
        IMMORTAL = 1 << 2,
    }
    [CTypeExport("gc_color_t")]
    public enum GCColor
    {
        RED,
        YELLOW,
        GREEN,
    }
}
