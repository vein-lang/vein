namespace wave.fs.elf
{
    public enum ElfType : ushort
    {
        None = 0,
        Relocatable = 1,
        Executable = 2,
        SharedObject = 3,
        Core = 4,
        LoProc = 5,
        HiProc = 6
    }
}
