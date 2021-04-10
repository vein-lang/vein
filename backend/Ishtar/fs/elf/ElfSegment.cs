namespace wave.fs.elf
{
    public struct ElfSegment
    {

        public ElfSegmentType Type;

        public uint Offset;

        public uint VirtualAddress;

        public uint PhysicalAddress;

        public uint FileSize;

        public uint MemorySize;

        public ElfSegmentFlags Flags;

        public uint Align;

    }
}
