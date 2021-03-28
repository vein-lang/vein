namespace wc_test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using insomnia;
    using insomnia.emit;
    using insomnia.fs;
    using Xunit;

    public class module_test
    {
        
        public static List<WaveModule> GetDeps()
        {
            var list = new List<WaveModule>();


            var stl = new WaveModuleBuilder("stl", new Version(2,3));

            foreach (var type in WaveCore.Types.All) 
                stl.GetTypeConstant(type.FullName);
            foreach (var type in WaveCore.All)
            {
                stl.GetTypeConstant(type.FullName);
                stl.GetStringConstant(type.Name);
                stl.GetStringConstant(type.Path);
                foreach (var field in type.Fields)
                {
                    stl.GetTypeConstant(field.FullName);
                    stl.GetStringConstant(field.Name);
                }
                foreach (var method in type.Methods)
                {
                    stl.GetStringConstant(method.Name);
                    foreach (var argument in method.Arguments)
                    {
                        stl.GetStringConstant(argument.Name);
                    }
                }
                stl.classList.Add(type);
            }
            list.Add(stl);
            return list;
        }
        [Fact]
        public void WriteTest()
        {
            var ver = new Version(2, 2, 2, 2);
            var module = new WaveModuleBuilder("blank", ver);
            module.Deps.AddRange(GetDeps());


            var @class = module.DefineClass("blank%global::wave/lang/SR");


            @class.Flags = ClassFlags.Public | ClassFlags.Static;
            var method = @class.DefineMethod("blank", MethodFlags.Public | MethodFlags.Static,
                WaveTypeCode.TYPE_VOID.AsType());

            var gen = method.GetGenerator();
            
            gen.Emit(OpCodes.NOP);

            module.BakeByteArray();
            module.BakeDebugString();

            var blank = new InsomniaAssembly (module) { Name = "blank", Version = ver};
            

            InsomniaAssembly.WriteTo(blank, new DirectoryInfo("C:/wave-lang-temp"));
        }
        
        //[Fact]
        public void ReaderTest()
        {
            var deps = GetDeps();
            var f = InsomniaAssembly.LoadFromFile("C:\\Program Files (x86)\\WaveLang\\sdk\\0.1-preview\\runtimes\\any\\stl.wll");
            var (_, bytes) = f.Sections.First();

            var result = ModuleReader.Read(bytes, deps, null);
            
            
            Assert.Equal("foo", result.Name);
            Assert.NotEmpty(result.classList);
            var @class = result.classList.First();
            Assert.Equal("baz", @class.Name);
            Assert.Equal("foo%global::soo/baz", @class.FullName.fullName);
            Assert.NotEmpty(@class.Fields);
            Assert.NotEmpty(@class.Methods);
            var field = @class.Fields.First();
            var method = @class.Methods.First();
            
            Assert.Equal("baz.xuy", field.FullName.fullName);
            Assert.Equal("sat", method.Name);
        }
    }
}