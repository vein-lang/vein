namespace wc_test
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sprache;
    using wave.emit;
    using wave.stl;
    using wave.syntax;
    using Xunit;
    public class FetchWaveSource : IEnumerable<object[]>
    {
        public const string RootOfWaveStd = "./../../../../../wave.std";
        
        public IEnumerator<object[]> GetEnumerator() => 
            Directory.EnumerateFiles($"{RootOfWaveStd}", "*.wave", SearchOption.AllDirectories)
                .Select(x => new object[]{x}).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class stl_compilation_test
    {
        
        [Theory]
        [ClassData(typeof(FetchWaveSource))]
        public void FilesParse(string path)
        {
            var code = File.ReadAllText(path);
            Wave.CompilationUnit.End().ParseWave(code);
        }

        [Fact]
        public void FilesCompile()
        {
            var code = File.ReadAllText($"{FetchWaveSource.RootOfWaveStd}/wave/lang/Object.wave");
            var doc = Wave.CompilationUnit.End().ParseWave(code);
            var module = new WaveModuleBuilder("wcorlib");
            //doc.CompileInto(module);
        }

        public WaveSyntax Wave => new();
    }
}