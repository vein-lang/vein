namespace wave.fs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using BinaryTools.Elf;
    using static BinaryTools.Elf.ElfSectionType;


    public static class WEx
    {
        public static bool Exist(this ElfSectionHeaderTable table, ElfSectionType type) 
            => table.Any(x => x.Type == type);
    }

    public partial class InsomniaAssembly
    {
        public string Name { get; init; }
        
        public List<(string name, byte[] data)> sections { get; set; } = new ();
        
        public void AddSegment((string name, byte[] data) seg) => sections.Add(seg);

        /// <exception cref="BadImageFormatException"/>
        public static InsomniaAssembly LoadFromFile(string file)
        {
            using var fs = File.OpenRead(file);
            using var br = new BinaryReader(fs);
            var elf = ElfFile.ReadElfFile(br);

            if (elf.Sections.All(x => x.Type != Note))
                throw new BadImageFormatException($"File '{file}' has invalid.", 
                    new ImageSegmentNotFoundException("elf .note segment not found."));
            if (elf.Sections.All(x => x.Type != ProgBits))
                throw new BadImageFormatException($"File '{file}' has invalid.", 
                    new ImageSegmentNotFoundException("elf .progBits segment not found."));
            
            var noteSection = elf.Sections.Single(x => x is { Type: Note });
            var keyCode = Encoding.ASCII.GetString(noteSection.ReadFrom(fs));
            
            if (keyCode != "wave")
                throw new BadImageFormatException($"File '{file}' is not wave image.");
            
            
            var ilCodeSection = elf.Sections.Single(x => x is { Type: ProgBits });

            using var memory = new MemoryStream(ilCodeSection.ReadFrom(fs));
            using var reader = new BinaryReader(memory);


            var sectionCount = reader.ReadInt32();
            var sections = new List<(string name, byte[] body)>();
            foreach (var i in ..sectionCount)
            {
                var len = reader.ReadInt32();
                var bytes = reader.ReadBytes(len);
                var str = Encoding.ASCII.GetString(bytes);
                sections.Add((str, null));
            }

            foreach (var i in ..sectionCount)
            {
                var tmp = sections[i];
                sections[i] = (tmp.name, reader.ReadBytes(reader.ReadInt32()));
            }
            
            return new InsomniaAssembly {sections = sections};
        }
        
        public static void WriteToFile(InsomniaAssembly asm, string file)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            
            writer.Write(asm.sections.Count);
            foreach (var (name, _) in asm.sections)
            {
                var b = Encoding.ASCII.GetBytes(name);
                writer.Write(b.Length);
                writer.Write(b);
            }

            foreach (var (_, data) in asm.sections)
            {
                writer.Write(data.Length);
                writer.Write(data);
            }

            using var fs = File.Create(file);
            
            WriteElf(memory.ToArray(), fs);
        }
    }
}