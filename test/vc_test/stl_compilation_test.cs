namespace wc_test
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sprache;
    using ishtar.emit;
    using vein.stl;
    using vein.syntax;
    using NUnit.Framework;
    public class FetchManaSource : IEnumerable<object[]>
    {
        public const string RootOfManaStd = "./../../../../../wave.std";

        public IEnumerator<object[]> GetEnumerator() =>
            Directory.EnumerateFiles($"{RootOfManaStd}", "*.wave", SearchOption.AllDirectories)
                .Select(x => new object[] { x }).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class stl_compilation_test
    {

        [Theory, Ignore("MANUAL")]
        // [ClassData(typeof(FetchManaSource))]
        public void FilesParse(string path)
        {
            var code = File.ReadAllText(path);
            Vein.CompilationUnit.End().ParseVein(code);
        }

        [Test, Ignore("MANUAL")]
        public void FilesCompile()
        {
            var code = File.ReadAllText($"{FetchManaSource.RootOfManaStd}/wave/lang/Object.wave");
            var doc = Vein.CompilationUnit.End().ParseVein(code);
            var module = new ManaModuleBuilder("wcorlib");
            //doc.CompileInto(module);
        }

        public VeinSyntax Vein => new();
    }
}
