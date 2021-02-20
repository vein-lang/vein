namespace wc_test
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using wave;
    using wave.emit;
    using wave.fs;
    using Xunit;

    public class module_test
    {
        
        public static List<WaveModule> GetDeps()
        {
            var list = new List<WaveModule>();


            var stl = new ModuleBuilder("stl");

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
        public ModuleBuilder WriteTest()
        {
            var module = new ModuleBuilder("foo");
            module.Deps.AddRange(GetDeps());


            var @class = module.DefineClass("global::soo/baz");


            @class.Flags = ClassFlags.Public | ClassFlags.Static;
            var method = @class.DefineMethod("sat", MethodFlags.Public | MethodFlags.Static,
                WaveTypeCode.TYPE_VOID.AsType());

            
            @class.Fields.Add(new WaveField(@class, "baz.xuy", FieldFlags.Static, WaveTypeCode.TYPE_I4.AsType()));

            var gen = method.GetGenerator();
            
            gen.Emit(OpCodes.NOP);

            module.BakeByteArray();
            module.BakeDebugString();

            return module;
        }
        
        [Fact]
        public void ReaderTest()
        {
            var deps = GetDeps();
            var module = WriteTest();
            var f = InsomniaAssembly.LoadFromFile("C:\\Program Files (x86)\\WaveLang\\sdk\\0.1-preview\\runtimes\\any\\stl.wll");
            var (_, bytes) = f.sections.First();

            var result = ModuleReader.Read(bytes, deps);
            
            
            Assert.Equal("foo", result.Name);
            Assert.NotEmpty(result.classList);
            var @class = result.classList.First();
            Assert.Equal("baz", @class.Name);
            Assert.Equal("global::soo/baz", @class.FullName.fullName);
            Assert.NotEmpty(@class.Fields);
            Assert.NotEmpty(@class.Methods);
            var field = @class.Fields.First();
            var method = @class.Methods.First();
            
            Assert.Equal("baz.xuy", field.FullName.fullName);
            Assert.Equal("sat", method.Name);
        }
    }
}