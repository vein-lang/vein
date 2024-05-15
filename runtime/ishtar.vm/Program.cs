using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ishtar;
using ishtar.runtime;
using vein.fs;
using vein.runtime;

unsafe
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        Console.OutputEncoding = Encoding.Unicode;

    //BoehmGCLayout.Native.GC_set_find_leak(true);
    BoehmGCLayout.Native.GC_init();
    
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
            vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, "0x1 [module path is not passed]", vm.Frames.EntryPoint);
            return -1;
        }
        var entry = new FileInfo(args.First());
        if (!entry.Exists)
        {
            vm.FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"0x2 [{entry} is not found]", vm.Frames.EntryPoint);
            return -2;
        }
        vault.WorkDirectory = entry.Directory;
        resolver = vault.GetResolver();
        masterModule = IshtarAssembly.LoadFromFile(entry);
        resolver.AddSearchPath(entry.Directory);
    }


    var module = resolver.Resolve(masterModule);

    module->class_table->ForEach(x => x->init_vtable(vm));

    //using var enumerator = module->class_table->_nativeData->GetEnumerator();

    //while (enumerator.MoveNext())
    //{
    //    var _clazz = (RuntimeIshtarClass*)enumerator.Current;
    //    _clazz->init_vtable(vm);
    //}


    var entry_point = module->GetEntryPoint();

    if (entry_point is null)
    {
        vm.FastFail(WNE.MISSING_METHOD, $"Entry point in '{module->Name}' module is not defined.", vm.Frames.EntryPoint);
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
        Console.WriteLine($"unhandled exception '{frame.exception.value->clazz->Name}' was thrown. \n" +
                          $"{frame.exception.stack_trace}");
        Console.ForegroundColor = ConsoleColor.White;
    }

    watcher.Stop();
    Console.WriteLine($"Elapsed: {watcher.Elapsed}");



    var s1 = vm.GC.AllocObject(vm.Types->ExceptionClass, frame);

    var isMarked = BoehmGCLayout.Native.GC_is_marked(s1);

    var size = BoehmGCLayout.Native.GC_size(s1);


    BoehmGCLayout.Native.GC_gcollect();

    var isMarked2 = BoehmGCLayout.Native.GC_is_marked(s1);

    var size2 = BoehmGCLayout.Native.GC_size(s1);

    vm.Dispose();

    var isMarked3 = BoehmGCLayout.Native.GC_is_marked(s1);


    var invalid = NativeMemory.AllocZeroed(100);
    var size3 = BoehmGCLayout.Native.GC_is_visible(invalid);


    return 0;


    static void my_is_visible_print_proc(void *ptr) {
        Console.WriteLine($"Proc");
        //int visible = GC_is_visible(ptr);
        //printf("Object at %p is visible: %d\n", ptr, visible);
    }
}


public record FooBarClass(string data, string anus);
