namespace insomnia.fs.elf
{
    public struct ElfHeader
    {

        public ElfIdentification Identification;

        public ElfType Type;

        public ushort Machine;

        public uint Version;

        public uint Entry;

        public uint ProgramHeaderOffset;

        public uint SectionHeaderOffset;

        public uint Flags;

        public ushort ElfHeaderSize;

        public ushort ProgramHeaderEntrySize;

        public ushort ProgramHeaderCount;

        public ushort SectionHeaderEntrySize;

        public ushort SectionHeaderCount;

        public ushort StringSectionIndex;

    }
}
