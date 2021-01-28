namespace wc_test
{
    using System.IO;
    using System.Text;
    using Xunit;
    using wave.fs.elf;
    public class elf_test
    {
        [Fact]
        public void ElfReadTest()
        {
            using var mem = new MemoryStream(4096 * 10);

            using var writer = new BinaryWriter(mem);

            var file = new ElfFile();
            file.Sections.Add(new ElfSection());
            AddCode(file);
            AddData(file);

            var sectionsStringsIndex = file.AddStringsSection();

            const int headerSize = 0x34;
            const int segmentsOffset = headerSize;
            const int segmentEntrySize = 0x20;
            var dataOffset = (uint)(segmentsOffset + file.Segments.Count * segmentEntrySize);
            var sectionsOffset = dataOffset + file.Data.Length;
            var header = new ElfHeader
            {
                Identification = {
                    Magic = new[] { (char)0x7f, 'E', 'L', 'F' },
                    FileClass = ElfFileClass.Elf32,
                    DataType = ElfDataType.Lsb,
                    Version = 1,
                },
                Type = ElfType.Executable,
                Machine = 0x0,
                Version = 1,
                Entry = 0x0,
                ProgramHeaderOffset = segmentsOffset,
                SectionHeaderOffset = (uint)sectionsOffset,
                Flags = 0x84,
                ElfHeaderSize = headerSize,
                ProgramHeaderEntrySize = segmentEntrySize,
                ProgramHeaderCount = (ushort)file.Segments.Count,
                SectionHeaderEntrySize = 0x28,
                SectionHeaderCount = (ushort)file.Sections.Count,
                StringSectionIndex = (ushort)sectionsStringsIndex
            };
            writer.WriteElf32(header);
            foreach (var segment in file.Segments)
            {
                var cloned = segment;
                cloned.Offset += dataOffset;
                writer.WriteElf32(cloned);
            }
            writer.Write(file.Data.ToArray());
            foreach (var section in file.Sections)
            {
                var cloned = section;
                if (section.Type != ElfSectionType.Null)
                {
                    cloned.Offset += dataOffset;
                }
                writer.WriteElf32(cloned);
            }



            var result = mem.ToArray();

            var stream = new MemoryStream(result);
            var reader = new BinaryTools.Elf.Io.EndianBinaryReader(stream, BinaryTools.Elf.Io.EndianBitConverter.NativeEndianness);
            var elfFile = BinaryTools.Elf.ElfFile.ReadElfFile(reader);
            
            
            File.WriteAllBytes(@"C:\Users\ls-mi\Desktop\auto_tracer\foo.elf", result);
            
        }

        private void AddCode(ElfFile file)
        {
            var code = Encoding.UTF8.GetBytes(".add;.sum;.swap;");

            file.Sections.Add(new ElfSection
            {
                Name = file.Strings.SaveString(".text"),
                Type = ElfSectionType.ProgBits,
                Address = 0,
                Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
                Size = (uint)code.Length,
                Align = 2,
                Offset = (uint)file.Data.Position
            });
            file.Segments.Add(new ElfSegment
            {
                Type = ElfSegmentType.Load,
                Offset = (uint)file.Data.Position,
                VirtualAddress = 0,
                PhysicalAddress = 0,
                FileSize = (uint)code.Length,
                MemorySize = (uint)code.Length,
                Flags = ElfSegmentFlags.Executable | ElfSegmentFlags.Readable,
                Align = 1
            });
            file.Data.Write(code, 0, code.Length);
        }

        private void AddData(ElfFile file)
        {
            var code = Encoding.UTF8.GetBytes("the big string data; also foo string");
            file.Sections.Add(new ElfSection
            {
                Name = file.Strings.SaveString(".bss"),
                Type = ElfSectionType.NoBits,
                Address = 0x800060,
                Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Writeable,
                Size = (uint)code.Length,
                Align = 1,
                Offset = (uint)file.Data.Position
            });
            file.Segments.Add(new ElfSegment
            {
                Type = ElfSegmentType.Load,
                Offset = (uint)file.Data.Position,
                VirtualAddress = 0x800060,
                PhysicalAddress = 0x800060,
                FileSize = (uint)code.Length,
                MemorySize = (uint)code.Length,
                Flags = ElfSegmentFlags.Writeable | ElfSegmentFlags.Readable,
                Align = 1,
            });
            file.Data.Write(code, 0, code.Length);
        }
    }
}