namespace mana.fs.elf
{
    public enum ElfSegmentType : uint
    {
        Null = 0,
        Load = 1,
        Dynamic = 2,
        Interp = 3,
        Note = 4,
        SharedLib = 5,
        ProgramHeaders = 6,
        LoProc = 0x70000000,
        HiProc = 0x7fffffff,
    }
}
