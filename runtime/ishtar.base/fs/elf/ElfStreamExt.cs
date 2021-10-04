namespace vein.fs.elf
{
    using System.IO;

    internal static class ElfStreamExt
    {

        public static ushort ReadElf32Half(this BinaryReader reader)
            => reader.ReadUInt16();

        public static void WriteElf32Half(this BinaryWriter writer, ushort val)
            => writer.Write(val);

        public static void WriteElf32Word(this BinaryWriter writer, uint val)
            => writer.Write(val);

        public static void WriteElf32Addr(this BinaryWriter writer, uint val)
            => writer.Write(val);

        public static void WriteElf32Off(this BinaryWriter writer, uint val)
            => writer.Write(val);

        public static void WriteElf32(this BinaryWriter writer, ElfIdentification identification)
        {
            foreach (var ch in identification.Magic)
                writer.Write((byte)ch);
            writer.Write((byte)identification.FileClass);
            writer.Write((byte)identification.DataType);
            writer.Write(identification.Version);
            var pad = new byte[9];
            writer.Write(pad);
        }

        public static void WriteElf32(this BinaryWriter writer, ElfHeader header)
        {
            writer.WriteElf32(header.Identification);
            writer.WriteElf32Half((ushort)header.Type);
            writer.WriteElf32Half(header.Machine);
            writer.WriteElf32Word(header.Version);
            writer.WriteElf32Addr(header.Entry);
            writer.WriteElf32Off(header.ProgramHeaderOffset);
            writer.WriteElf32Off(header.SectionHeaderOffset);
            writer.WriteElf32Word(header.Flags);
            writer.WriteElf32Half(header.ElfHeaderSize);
            writer.WriteElf32Half(header.ProgramHeaderEntrySize);
            writer.WriteElf32Half(header.ProgramHeaderCount);
            writer.WriteElf32Half(header.SectionHeaderEntrySize);
            writer.WriteElf32Half(header.SectionHeaderCount);
            writer.WriteElf32Half(header.StringSectionIndex);
        }

        public static void WriteElf32(this BinaryWriter writer, ElfSegment segment)
        {
            writer.WriteElf32Word((uint)segment.Type);
            writer.WriteElf32Off(segment.Offset);
            writer.WriteElf32Addr(segment.VirtualAddress);
            writer.WriteElf32Addr(segment.PhysicalAddress);
            writer.WriteElf32Word(segment.FileSize);
            writer.WriteElf32Word(segment.MemorySize);
            writer.WriteElf32Word((uint)segment.Flags);
            writer.WriteElf32Word(segment.Align);
        }

        public static void WriteElf32(this BinaryWriter writer, ElfSection section)
        {
            writer.WriteElf32Word(section.Name);
            writer.WriteElf32Word((uint)section.Type);
            writer.WriteElf32Word((uint)section.Flags);
            writer.WriteElf32Addr(section.Address);
            writer.WriteElf32Off(section.Offset);
            writer.WriteElf32Word(section.Size);
            writer.WriteElf32Word(section.Link);
            writer.WriteElf32Word(section.Info);
            writer.WriteElf32Word(section.Align);
            writer.WriteElf32Word(section.EntrySize);
        }
    }
}
