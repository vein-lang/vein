namespace mana.fs.elf
{
    public struct ElfSection
    {

        public uint Name;

        public ElfSectionType Type;

        public ElfSectionFlags Flags;

        public uint Address;

        public uint Offset;

        public uint Size;

        public uint Link;

        public uint Info;

        public uint Align;

        public uint EntrySize;

    }
}
