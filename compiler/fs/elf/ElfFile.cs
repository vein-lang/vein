namespace wave.fs.elf
{
    using System.Collections.Generic;
    using System.IO;
    public class ElfFile
    {

        private readonly List<ElfSection> _sections = new();
        private readonly List<ElfSegment> _segments = new();
        private readonly ElfStrings _strings = new();

        private readonly MemoryStream _data = new();

        public List<ElfSection> Sections => _sections;

        public List<ElfSegment> Segments => _segments;

        public MemoryStream Data => _data;

        public ElfStrings Strings => _strings;


        public int AddStringsSection()
        {
            _sections.Add(new ElfSection
            {
                Name = _strings.SaveString(".shstrtab"),
                Type = ElfSectionType.StrTab,
                Address = 0,
                Flags = ElfSectionFlags.None,
                Size = (uint)_strings.BytesSize,
                Align = 1,
                Offset = (uint)Data.Position
            });
            _data.Write(_strings.ToArray(), 0, _strings.BytesSize);
            return _sections.Count - 1;
        }
    }
}
