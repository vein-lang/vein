using System;

namespace mana.fs.elf
{
    [Flags]
    public enum ElfSegmentFlags
    {
        Executable = 1,
        Writeable = 2,
        Readable = 4
    }
}
