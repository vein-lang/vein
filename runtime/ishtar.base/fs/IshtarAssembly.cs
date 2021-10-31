namespace vein.fs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using BinaryTools.Elf;
    using BinaryTools.Elf.Io;
    using extensions;
    using ishtar.emit;
    using static BinaryTools.Elf.ElfSectionType;


    public static class WEx
    {
        public static bool Exist(this ElfSectionHeaderTable table, ElfSectionType type)
            => table.Any(x => x.Type == type);

        public static string GetStringByKey(this ElfStringTable table, string key)
        {
            var value = table.Single(x => x.Value.StartsWith(key));

            return value.Value.Replace($"{key}::", "");
        }
    }

    public partial class IshtarAssembly
    {
        public string Name { get; init; }

        public Version Version
        {
            get => metadata.Version;
            set => metadata.Version = value;
        }

        public List<(string name, byte[] data)> Sections { get; set; } = new();

        private string DebugData = "";

        public void AddSegment((string name, byte[] data) seg) => Sections.Add(seg);

        private InsomniaAssemblyMetadata metadata = new ();

        private void InsertModule(VeinModuleBuilder module)
        {
            if (module is null)
                throw new ArgumentNullException(nameof(module));
            if (string.IsNullOrEmpty(module.Name))
                throw new ArgumentNullException($"module.Name");
            Version = module.Version;
            this.AddSegment((".code", module.BakeByteArray()));
            DebugData = module.BakeDebugString();
        }


        public IshtarAssembly(VeinModuleBuilder module)
            => this.InsertModule(module);

        internal IshtarAssembly()
        {
        }

        public static bool TryLoadFromFile(FileInfo file, out IshtarAssembly result, out Exception exception)
        {
            try
            {
                result = LoadFromFile(file.FullName);
                exception = null;
                return true;
            }
            catch (BadImageFormatException e)
            {
                exception = e;
                result = null;
                return false;
            }
        }
        /// <exception cref="BadImageFormatException"/>
        public static IshtarAssembly LoadFromFile(FileInfo file)
            => LoadFromFile(file.FullName);
        /// <exception cref="BadImageFormatException"/>
        public static IshtarAssembly LoadFromMemory(MemoryStream stream)
            => LoadFromStream(stream, "unnamed-module.wll");

        /// <exception cref="BadImageFormatException"/>
        private static IshtarAssembly LoadFromStream(Stream stream, string name)
        {
            using var br = new BinaryReader(stream);
            var elf = default(ElfFile);

            try
            {
                elf = ElfFile.ReadElfFile(br);
            }
            catch (FileFormatException e)
            {
                throw new BadImageFormatException($"File '{name}' has invalid.", e);
            }

            if (elf.Sections.All(x => x.Type != Note))
                throw new BadImageFormatException($"File '{name}' has invalid.",
                    new ImageSegmentNotFoundException("elf .note segment not found."));
            if (elf.Sections.All(x => x.Type != ProgBits))
                throw new BadImageFormatException($"File '{name}' has invalid.",
                    new ImageSegmentNotFoundException("elf .progBits segment not found."));

            var noteSection = elf.Sections.Single(x => x is { Type: Note });
            var keyCode = Encoding.ASCII.GetString(noteSection.ReadFrom(stream));

            if (keyCode != "insomnia")
                throw new BadImageFormatException($"File '{name}' is not insomnia image.");

            var strings = elf.Sections.Single(x => x is { Type: StrTab }) as ElfStringTable;
            var metadata = new InsomniaAssemblyMetadata
            {
                Version = System.Version.Parse(strings.GetStringByKey(".wasm-version")),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(
                    long.Parse(strings.GetStringByKey(".wasm-timestamp")))
            };




            var ilCodeSection = elf.Sections.Single(x => x is { Type: ProgBits });

            using var memory = new MemoryStream(ilCodeSection.ReadFrom(stream));
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

            return new IshtarAssembly
            {
                Sections = sections,
                Name = Path.GetFileNameWithoutExtension(name),
                metadata = metadata
            };
        }
        /// <exception cref="BadImageFormatException"/>
        public static IshtarAssembly LoadFromFile(string file)
        {
            using var fs = File.OpenRead(file);
            return LoadFromStream(fs, file);
        }

        public void WriteTo(DirectoryInfo directory)
        {
            var file = new FileInfo(Path.Combine(directory.FullName, $"{this.Name}.wll"));
            WriteTo(this, file.FullName);
        }

        public static void WriteTo(IshtarAssembly asm, DirectoryInfo directory)
        {
            var file = new FileInfo(Path.Combine(directory.FullName, $"{asm.Name}.wll"));
            WriteTo(asm, file.FullName);
        }
        internal static void WriteTo(IshtarAssembly asm, string file)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);

            writer.Write(asm.Sections.Count);
            foreach (var (name, _) in asm.Sections)
            {
                var b = Encoding.ASCII.GetBytes(name);
                writer.Write(b.Length);
                writer.Write(b);
            }

            foreach (var (_, data) in asm.Sections)
            {
                writer.Write(data.Length);
                writer.Write(data);
            }

            using var fs = File.Create(file);

            WriteElf(memory.ToArray(), fs, asm.metadata);

            if (!string.IsNullOrEmpty(asm.DebugData))
                File.WriteAllText($"{file}.lay", asm.DebugData);
        }
    }
}
