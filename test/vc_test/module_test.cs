namespace wc_test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ishtar;
    using vein;
    using vein.fs;
    using ishtar.emit;
    using vein.project;
    using vein.runtime;
    using NUnit.Framework;

    public class module_test
    {

        public static List<VeinModule> GetDeps()
        {
            var list = new List<VeinModule>();


            var stl = new VeinModuleBuilder("stl", new Version(2,3));

            foreach (var type in VeinCore.All)
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
        [Test, Ignore("MANUAL")]
        public void WriteTest()
        {
            var verSR = new Version(2, 2, 2, 2);
            var moduleSR = new VeinModuleBuilder("set1", verSR);
            {
                moduleSR.Deps.AddRange(GetDeps());


                var @class = moduleSR.DefineClass("set1%global::wave/lang/SR");


                @class.Flags = ClassFlags.Public | ClassFlags.Static;
                var method = @class.DefineMethod("blank", MethodFlags.Public | MethodFlags.Static,
                    VeinTypeCode.TYPE_VOID.AsClass());

                var gen = method.GetGenerator();

                gen.Emit(OpCodes.NOP);

                moduleSR.BakeByteArray();
                moduleSR.BakeDebugString();

                var blank = new IshtarAssembly (moduleSR) { Name = "set1", Version = verSR};


                IshtarAssembly.WriteTo(blank, new DirectoryInfo("C:/wavelib"));
            }


            {
                var ver = new Version(2, 2, 2, 2);
                var module = new VeinModuleBuilder("set2", ver);
                module.Deps.AddRange(GetDeps());


                var @class = module.DefineClass("set2%global::wave/lang/DR");


                @class.Flags = ClassFlags.Public | ClassFlags.Static;
                var method = @class.DefineMethod("blank", MethodFlags.Public | MethodFlags.Static,
                    VeinTypeCode.TYPE_VOID.AsClass());

                var gen = method.GetGenerator();

                gen.Emit(OpCodes.NOP);

                module.BakeByteArray();
                module.BakeDebugString();

                module.Deps.Add(moduleSR);

                var blank = new IshtarAssembly (module) { Name = "set2", Version = ver};


                IshtarAssembly.WriteTo(blank, new DirectoryInfo("C:/wavelib"));
            }
        }

        [Test, Ignore("MANUAL")]
        public void ReadTest()
        {
            //var target = new FileInfo(@"C:/wavelib/set2.wll");

            //var insm = IshtarAssembly.LoadFromFile(target);
            //var deps = GetDeps();

            //var (_, bytes) = insm.Sections.First();

            //var sdk = new VeinSDK(new VeinProject(new FileInfo(@"C:\wavelib\foo.ww"), new XML.Project()
            //{
            //    Sdk = "default"
            //}));


            // var resolver = new AssemblyResolver(sdk.RootPath);

            //var result = ModuleReader.Read(bytes, deps, (x,z) => resolver.ResolveDep(x,z,deps));
        }

        [Test, Ignore("MANUAL")]
        public void ReaderTest()
        {
            //var deps = GetDeps();
            //var f = IshtarAssembly.LoadFromFile(@"C:\Program Files (x86)\ManaLang\sdk\0.1-preview\std\aspera.wll");
            //var (_, bytes) = f.Sections.First();

            //var sdk = new VeinSDK(new VeinProject(new FileInfo(@"C:\wave-lang-temp\foo.ww"), new XML.Project()
            //{
            //    Sdk = "default"
            //}));



            //var result = ModuleReader.Read(bytes, deps, (x,z) => sdk.ResolveDep(x,z,deps));


            //Assert.AreEqual("aspera", result.Name);
            //IshtarAssert.NotEmpty(result.class_table);
            //var @class = result.class_table.First();
            //Assert.AreEqual("DR", @class.Name);
            //Assert.AreEqual("aspera%global::wave/lang/DR", @class.FullName.fullName);
            //IshtarAssert.NotEmpty(@class.Methods);
            //var method = @class.Methods.First();
            //Assert.AreEqual("blank()", method.Name);
        }
    }
}
