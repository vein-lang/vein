namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using fs;
    using ishtar;
    internal class Program
    {
        #if EXPERIMENTAL_JIT
        public unsafe static void TestJitFunctional()
        {
            var unused = default(NativeApi.Protection);
            var ptr = NativeLibrary.Load("sample_native_library.dll");
            var fnPtr2 = NativeLibrary.GetExport(ptr, "_sample_2");
            var result1 = 0;
            var result2 = 15;
            int* mem1 = &result1;
            void* mem2 = &result2;

            var bw = mem1[0];


            var qwe = IshtarJIT.WrapNativeCall((IntPtr)0);

            ((delegate*<void>)IshtarJIT.WrapNativeCall_WithArg_Int32(fnPtr2.ToPointer(), 127))();

            ((delegate*<int, void>)IshtarJIT.WrapNativeCall(fnPtr5.ToPointer(), &mem))(55);
            var b = *((int*)mem1);
            var c = *((int*)mem2);
        }
        #endif

        public static unsafe int Main(string[] args)
        {
            //TestJitFunctional();
            //while (!Debugger.IsAttached)
            //    Thread.Sleep(200);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.OutputEncoding = Encoding.Unicode;
            var vm = VirtualMachine.Create("app");
            var vault = vm.Vault;

            var masterModule = default(IshtarAssembly);
            var resolver = default(AssemblyResolver);

            if (AssemblyBundle.IsBundle(out var bundle))
            {
                resolver = vault.GetResolver();
                masterModule = bundle.Assemblies.First();
                resolver.AddInMemory(bundle);
            }
            else
            {
                if (args.Length < 1)
                {
                    vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, "0x1", vm.Frames.EntryPoint);
                    return -1;
                }
                var entry = new FileInfo(args.First());
                if (!entry.Exists)
                {
                    vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, "0x2", vm.Frames.EntryPoint);
                    return -2;
                }
                vault.WorkDirecotry = entry.Directory;
                resolver = vault.GetResolver();
                masterModule = IshtarAssembly.LoadFromFile(entry);
                resolver.AddSearchPath(entry.Directory);
            }


            var module = resolver.Resolve(masterModule);

            foreach (var @class in module.class_table.OfType<RuntimeIshtarClass>())
                @class.init_vtable(vm);

            var entry_point = module.GetEntryPoint();

            if (entry_point is null)
            {
                vm.FastFail(WNE.MISSING_METHOD, $"Entry point in '{module.Name}' module is not defined.", vm.Frames.EntryPoint);
                return -280;
            }

            var args_ = stackalloc stackval[1];

            var frame = new CallFrame(vm)
            {
                args = args_,
                method = entry_point,
                level = 0
            };
            

            var watcher = Stopwatch.StartNew();
            vm.exec_method(frame);

            if (frame.exception is not null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"unhandled exception '{frame.exception.value->decodeClass().Name}' was thrown. \n" +
                                  $"{frame.exception.stack_trace}");
                Console.ForegroundColor = ConsoleColor.White;
            }

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
                //VM.FastFail(WNE.STATE_CORRUPT, "Current executable has corrupted. [process file not found]", IshtarFrames.EntryPoint);
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
