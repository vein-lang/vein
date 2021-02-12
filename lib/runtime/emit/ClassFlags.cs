namespace wave.emit
{
    using System;

    [Flags]
    public enum ClassFlags : byte
    {
        None = 0,
        Public = 1 << 0,
        Static = 1 << 1,
        Internal = 1 << 2,
        Protected = 1 << 3,
        Private = 1 << 4
    }
}