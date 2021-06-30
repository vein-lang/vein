namespace mana.runtime
{
    using System;

    [Flags]
    public enum ClassFlags : short
    {
        None        = 0 << 0,
        Public      = 1 << 1,
        Static      = 1 << 2,
        Internal    = 1 << 3,
        Protected   = 1 << 4,
        Private     = 1 << 5,
        Abstract    = 1 << 6,
        Special     = 1 << 7
    }
}
