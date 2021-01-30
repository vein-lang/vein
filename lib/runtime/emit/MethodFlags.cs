namespace wave.emit
{
    using System;

    [Flags]
    public enum MethodFlags : sbyte
    {
        Public = 1 << 0,
        Static = 1 << 1,
        Internal = 1 << 2,
        Protected = 1 << 3,
        Private = 1 << 4,
        Extern = 1 << 5
    }
}