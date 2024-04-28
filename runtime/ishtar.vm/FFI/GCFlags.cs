namespace ishtar
{
    using System;

    [Flags]
    public enum GCFlags
    {
        NONE = 0,
        NATIVE_REF = 1 << 1,
        IMMORTAL = 1 << 2,
    }

    public enum GCColor
    {
        RED,
        YELLOW,
        GREEN,
    }
}
