namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Flags]
    public enum VeinMemberKind
    {
        Ctor     = 1 << 1,
        Dtor     = 1 << 2,
        Field    = 1 << 3,
        Method   = 1 << 4,
        Type     = 1 << 5,
        Property = 1 << 6
    }
}
