namespace vein.fs.elf
{
    using System;

    [Flags]
    public enum ElfSegmentFlags
    {
        Executable = 1,
        Writeable = 2,
        Readable = 4
    }
}
