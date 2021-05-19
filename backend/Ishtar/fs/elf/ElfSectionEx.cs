namespace mana.fs
{
    using System.IO;

    public static class ElfSectionEx
    {
        public static byte[] ReadFrom(this BinaryTools.Elf.ElfSection section, Stream stream)
        {
            var pos = stream.Position;
            stream.Seek((int) section.Offset, SeekOrigin.Begin);
            var arr = new byte[section.Size];
            stream.Read(arr);
            stream.Seek(pos, SeekOrigin.Begin);
            return arr;
        }
    }
}