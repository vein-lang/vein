namespace wave.fs
{
    using System.IO;
    using System.Text;
    using elf;
    using ElfFile = elf.ElfFile;
    using ElfHeader = elf.ElfHeader;
    using ElfSection = elf.ElfSection;
    using ElfSectionFlags = elf.ElfSectionFlags;
    using ElfSectionType = elf.ElfSectionType;
    using ElfSegment = elf.ElfSegment;
    using ElfSegmentFlags = elf.ElfSegmentFlags;
    using ElfSegmentType = elf.ElfSegmentType;
    using ElfType = elf.ElfType;
    public partial class WaveAssembly
    {
        protected internal static void WriteElf(byte[] ilCode, Stream stream)
        {
            using var writer = new BinaryWriter(stream);

            var file = new ElfFile();
            file.Sections.Add(new ElfSection());
            AddCode(file, ilCode);

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
                Type = ElfType.SharedObject,
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
        }
        
        private static void AddCode(ElfFile file, byte[] il)
        {
            file.Sections.Add(new ElfSection
            {
                Name = file.Strings.SaveString(".il_code"),
                Type = ElfSectionType.ProgBits,
                Address = 0,
                Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Executable,
                Size = (uint)il.Length,
                Align = 2,
                Offset = (uint)file.Data.Position
            });
            file.Segments.Add(new ElfSegment
            {
                Type = ElfSegmentType.Load,
                Offset = (uint)file.Data.Position,
                VirtualAddress = 0,
                PhysicalAddress = 0,
                FileSize = (uint)il.Length,
                MemorySize = (uint)il.Length,
                Flags = ElfSegmentFlags.Executable | ElfSegmentFlags.Readable,
                Align = 1
            });
            file.Data.Write(il, 0, il.Length);
            
            var vm_notes = Encoding.ASCII.GetBytes("wave");
            file.Sections.Add(new ElfSection
            {
                Name = file.Strings.SaveString(".note"),
                Type = ElfSectionType.Note,
                Address = 0,
                Flags = ElfSectionFlags.Alloc | ElfSectionFlags.Writeable,
                Size = (uint)vm_notes.Length,
                Align = 1,
                Offset = (uint)file.Data.Position
            });
            file.Segments.Add(new ElfSegment
            {
                Type = ElfSegmentType.Note,
                Offset = (uint)file.Data.Position,
                VirtualAddress = 0,
                PhysicalAddress = 0,
                FileSize = (uint)vm_notes.Length,
                MemorySize = (uint)vm_notes.Length,
                Flags = ElfSegmentFlags.Readable,
                Align = 1
            });
            file.Data.Write(vm_notes, 0, vm_notes.Length);
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