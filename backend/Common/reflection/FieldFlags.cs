namespace wave.runtime
{
    using System;

    [Flags]
    public enum FieldFlags : short
    {
        None        = 0 << 0,
        Literal     = 1 << 1,
        Public      = 1 << 2,
        Static      = 1 << 3,
        Protected   = 1 << 4,
        Virtual     = 1 << 5,
        Abstract    = 1 << 6,
        Override    = 1 << 7,
        Special     = 1 << 8,
        Readonly    = 1 << 9,
        Internal    = 1 << 10
    }
}