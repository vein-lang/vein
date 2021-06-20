namespace mana.backend.ishtar.light
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using fs;
    using global::ishtar;
    using runtime;
    using mana.ishtar.emit;


    internal class Program
    {
        private static void INIT_VTABLES()
        {
            foreach (var @class in ManaCore.All.OfType<RuntimeIshtarClass>())
                @class.init_vtable();
        }

        public static unsafe int Main(string[] args)
        {
            //while (!Debugger.IsAttached)
            //    Thread.Sleep(200);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.OutputEncoding = Encoding.Unicode;
            IshtarCore.INIT();
            INIT_VTABLES();
            IshtarGC.INIT();
            FFI.INIT();

            var masterModule = default(IshtarAssembly);
            var resolver = new AssemblyResolver();

            if (AssemblyBundle.IsBundle(out var bundle))
            {
                masterModule = bundle.Assemblies.First();
                resolver.AddInMemory(bundle);
            }
            else
            {
                if (args.Length < 1)
                    return -1;
                var entry = new FileInfo(args.First());
                if (!entry.Exists)
                    return -2;
                masterModule = IshtarAssembly.LoadFromFile(entry);
                resolver.AddSearchPath(entry.Directory);
            }

            var (_, code) = masterModule.Sections.First();

            resolver.AddSearchPath(new DirectoryInfo("/ManaLang"));
            resolver.AddSearchPath(new DirectoryInfo("./"));


            var module = RuntimeModuleReader.Read(code, new List<ManaModule>(), (s, version) =>
                resolver.ResolveDep(s, version, new List<ManaModule>()));

            foreach (var @class in module.class_table.OfType<RuntimeIshtarClass>())
            {
                @class.init_vtable();
                VM.ValidateLastError();
            }

            var entry_point = module.GetEntryPoint();

            if (entry_point is null)
            {
                VM.FastFail(WNE.MISSING_METHOD, "Entry point is not defined.");
                VM.ValidateLastError();
                return -280;
            }

            var args_ = stackalloc stackval[1];

            var frame = new CallFrame
            {
                args = args_,
                method = entry_point,
                level = 0
            };

            {// i don't know why
                IshtarCore.INIT_ADDITIONAL_MAPPING();
                INIT_VTABLES();
            }
            
            

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


    public class AssemblyBundle
    {
        public FileInfo MainModulePath { get; set; }
        public List<byte> MainModuleBytes { get; set; }

        public List<IshtarAssembly> Assemblies { get; private set; }


        public static bool IsBundle(out AssemblyBundle bundle)
        {
            var current = Process.GetCurrentProcess()?.MainModule?.FileName;
            bundle = null;
            if (string.IsNullOrEmpty(current))
            {
                VM.FastFail(WNE.STATE_CORRUPT, "Current executable has corrupted. [process file not found]");
                VM.ValidateLastError();
                return false;
            }

            var bytes = File.ReadAllBytes(current).ToList();
            var magicBytes = bytes.TakeLast(2).ToArray();

            if (BitConverter.ToInt16(magicBytes, 0) != 0x7ABC)
                return false;
            bundle = new AssemblyBundle
            {
                MainModuleBytes = bytes,
                MainModulePath = new FileInfo(current)
            }.UnpackAssemblies();

            return true;
        }


        private AssemblyBundle UnpackAssemblies()
        {
            Assemblies = new List<IshtarAssembly>();


            var offset_bytes = MainModuleBytes.SkipLast(sizeof(short)).TakeLast(sizeof(int)).ToArray();
            var offset = BitConverter.ToInt32(offset_bytes);

            var input = MainModuleBytes.SkipLast(sizeof(short) + sizeof(int)).Skip(offset).ToArray();
            using var mem = new MemoryStream(input); // todo multiple modules
            Assemblies.Add(IshtarAssembly.LoadFromMemory(mem));

            return this;
        }
    }
}
