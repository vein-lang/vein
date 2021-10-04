namespace vein.fs.elf
{
    public enum ElfSectionType : uint
    {
        Null = 0,
        ProgBits = 1,
        SymTab = 2,
        StrTab = 3,
        Rela = 4,
        Hash = 5,
        Dynamic = 6,
        Note = 7,
        NoBits = 8,
        Rel = 9,
        ShLib = 10,
        DynSym = 11,
        LoProc = 0x70000000,
        HiProc = 0x7fffffff,
        LoUser = 0x80000000,
        HiUser = 0xffffffff
    }
}
