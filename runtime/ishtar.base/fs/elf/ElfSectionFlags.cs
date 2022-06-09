namespace vein.fs.elf
{
    using System;

    [Flags]
    public enum ElfSectionFlags
    {
        None = 0,
        Writeable = 0x01,
        Alloc = 0x02,
        Executable = 0x04,
    }
}
