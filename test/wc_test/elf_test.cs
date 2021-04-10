namespace wc_test
{
    using System;
    using System.IO;
    using System.Text;
    using insomnia.emit;
    using wave.fs;
    using Xunit;
    public class ElfTest
    {
        [Fact]
        public void ElfReadTest()
        {
            var file = GetTempFile();
            var asm = new InsomniaAssembly
            {
                Name = "wave_test"
            };
            asm.AddSegment((".code", Encoding.ASCII.GetBytes("IL_CODE")));
            InsomniaAssembly.WriteTo(asm, file);
            var result = InsomniaAssembly.LoadFromFile(file);
            var (_, body) = result.Sections[0];
            Assert.Equal("IL_CODE", Encoding.ASCII.GetString(body));
            var f_mem = new MemoryStream(File.ReadAllBytes(file));
            f_mem.Seek(f_mem.Capacity - (sizeof(uint) * 2), SeekOrigin.Begin);
            var bin = new BinaryReader(f_mem);
            var len = bin.ReadUInt32();
            var offset = bin.ReadUInt32();
            f_mem.Seek(offset, SeekOrigin.Begin);
            var bytes = bin.ReadBytes((int)len);
            Assert.Equal("IL_CODE", Encoding.ASCII.GetString(bytes));
            File.Delete(file);
        }
        [Fact]
        public void ElfReadManual()
        {
            var file = @"C:\Users\ls-mi\Desktop\wave.elf";
            var asm = new InsomniaAssembly
            {
                Name = "wave_test"
            };
            asm.AddSegment((".code", Encoding.ASCII.GetBytes("IL_CODE")));
            InsomniaAssembly.WriteTo(asm, file);
            var result = InsomniaAssembly.LoadFromFile(file);
        }

        public string GetTempFile() => Path.Combine(Path.GetTempPath(), "wave_test", Path.GetTempFileName());
    }
}