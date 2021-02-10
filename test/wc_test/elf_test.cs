namespace wc_test
{
    using System;
    using System.IO;
    using System.Text;
    using wave.emit;
    using wave.fs;
    using Xunit;
    public class elf_test
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
            InsomniaAssembly.WriteToFile(asm, file);
            var result = InsomniaAssembly.LoadFromFile(file);
            var (_, body) = result.sections[0];
            Assert.Equal("IL_CODE", Encoding.ASCII.GetString(body));
            File.Delete(file);
        }
        //public void ElfReadManul()
        //{
        //    var file = @"C:\Users\ls-mi\Desktop\wave.elf";
        //    var asm = new InsomniaAssembly
        //    {
        //        Name = "wave_test"
        //    };
        //    asm.AddSegment((".code", Encoding.ASCII.GetBytes("IL_CODE")));
        //    InsomniaAssembly.WriteToFile(asm, file);
        //    var result = InsomniaAssembly.LoadFromFile(file);
        //}

        public string GetTempFile() => Path.Combine(Path.GetTempPath(), "wave_test", Path.GetTempFileName());
    }
}