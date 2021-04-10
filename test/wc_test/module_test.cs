namespace wc_test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using insomnia;
    using insomnia.emit;
    using wave.fs;
    using insomnia.project;
    using Xunit;

    public class module_test
    {
        
        public static List<WaveModule> GetDeps()
        {
            var list = new List<WaveModule>();


            var stl = new WaveModuleBuilder("stl", new Version(2,3));

            foreach (var type in WaveCore.Types.All) 
                stl.InternTypeName(type.FullName);
            foreach (var type in WaveCore.All)
            {
                stl.InternTypeName(type.FullName);
                stl.InternString(type.Name);
                stl.InternString(type.Path);
                foreach (var field in type.Fields)
                {
                    stl.InternFieldName(field.FullName);
                    stl.InternString(field.Name);
                }
                foreach (var method in type.Methods)
                {
                    stl.InternString(method.Name);
                    foreach (var argument in method.Arguments)
                    {
                        stl.InternString(argument.Name);
                    }
                }
                stl.class_table.Add(type);
            }
            list.Add(stl);
            return list;
        }
        [Fact]
        public void WriteTest()
        {
            var verSR = new Version(2, 2, 2, 2);
            var moduleSR = new WaveModuleBuilder("blank", verSR);
            {
                moduleSR.Deps.AddRange(GetDeps());


                var @class = moduleSR.DefineClass("blank%global::wave/lang/SR");


                @class.Flags = ClassFlags.Public | ClassFlags.Static;
                var method = @class.DefineMethod("blank", MethodFlags.Public | MethodFlags.Static,
                    WaveTypeCode.TYPE_VOID.AsType());

                var gen = method.GetGenerator();
            
                gen.Emit(OpCodes.NOP);

                moduleSR.BakeByteArray();
                moduleSR.BakeDebugString();

                var blank = new InsomniaAssembly (moduleSR) { Name = "blank", Version = verSR};
            

                InsomniaAssembly.WriteTo(blank, new DirectoryInfo("C:/wave-lang-temp"));
            }


            {
                var ver = new Version(2, 2, 2, 2);
                var module = new WaveModuleBuilder("aspera", ver);
                module.Deps.AddRange(GetDeps());


                var @class = module.DefineClass("aspera%global::wave/lang/DR");


                @class.Flags = ClassFlags.Public | ClassFlags.Static;
                var method = @class.DefineMethod("blank", MethodFlags.Public | MethodFlags.Static,
                    WaveTypeCode.TYPE_VOID.AsType());

                var gen = method.GetGenerator();
            
                gen.Emit(OpCodes.NOP);

                module.BakeByteArray();
                module.BakeDebugString();

                module.Deps.Add(moduleSR);

                var blank = new InsomniaAssembly (module) { Name = "aspera", Version = ver};
            

                InsomniaAssembly.WriteTo(blank, new DirectoryInfo("C:/wave-lang-temp"));
            }
        }
        
        [Fact]
        public void ReaderTest()
        {
            var deps = GetDeps();
            var f = InsomniaAssembly.LoadFromFile(@"C:\Program Files (x86)\WaveLang\sdk\0.1-preview\std\aspera.wll");
            var (_, bytes) = f.Sections.First();

            var sdk = new WaveSDK(new WaveProject(new FileInfo(@"C:\wave-lang-temp\foo.ww"), new XML.Project()
            {
                Sdk = "default"
            }));

            

            var result = ModuleReader.Read(bytes, deps, (x,z) => sdk.ResolveDep(x,z,deps));
            
            
            Assert.Equal("aspera", result.Name);
            Assert.NotEmpty(result.class_table);
            var @class = result.class_table.First();
            Assert.Equal("DR", @class.Name);
            Assert.Equal("aspera%global::wave/lang/DR", @class.FullName.fullName);
            Assert.NotEmpty(@class.Methods);
            var method = @class.Methods.First();
            Assert.Equal("blank()", method.Name);
        }
    }
}