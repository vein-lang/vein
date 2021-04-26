namespace wave.backend.ishtar.light
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using fs;
    using global::ishtar;
    using runtime;
    using wave.ishtar.emit;


    internal class Program
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
        public static unsafe int Main(string[] args)
        {
            IshtarCore.Init();
            if (args.Length < 1)
                return -1;
            var entry = new FileInfo(args.First());
            Console.WriteLine(args.First());
            if (!entry.Exists)
                return -2;
            var asm = IshtarAssembly.LoadFromFile(entry);
            var (_, code) = asm.Sections.First();
            var deps = GetDeps();

            var resolver = new AssemblyResolver();

            resolver.AddSearchPath(new DirectoryInfo("/WaveLang"));
            resolver.AddSearchPath(new DirectoryInfo("./"));

            var module = RuntimeModuleReader.Read(code, deps, (s, version) => 
                resolver.ResolveDep(s, version, deps));

            foreach (var @class in module.class_table.OfType<RuntimeIshtarClass>())
            {
                @class.init_vtable();
                VM.ValidateLastError();
            }

            module.Deps.Add(deps.First());

            var entry_point = module.GetEntryPoint();

            if (entry_point is null)
            {
                VM.FastFail(WaveNativeException.MISSING_METHOD, "Entry point is not defined.");
                var empty = stackalloc uint[1];
                entry_point = new RuntimeIshtarMethod("master", MethodFlags.Public | MethodFlags.Static)
                {
                    Header = new MetaMethodHeader
                    {
                        code = empty,
                        code_size = 1
                    }
                };
            }

            var args_ = stackalloc stackval[1];

            var frame = new CallFrame
            {
                args = args_, 
                method = entry_point, 
                level = 0
            };
            

            var watcher = Stopwatch.StartNew();
            VM.exec_method(frame);

            if (frame.exception is not null)
                Console.WriteLine($"unhandled exception was thrown. \n" +
                                  $"{frame.exception.stack_trace}");

            watcher.Stop();
            Console.WriteLine($"Elapsed: {watcher.Elapsed}");
            
            return 0;
        }
    }
}
