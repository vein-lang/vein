using System.Reflection;
using System.Text;
using ishtar;
using ishtar.io;
using ishtar.runtime;
using vein.fs;
using vein.runtime;

unsafe
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        Console.OutputEncoding = Encoding.Unicode;
    var bootCfg = VirtualMachine.readBootCfg();
    var appCfg = new AppConfig(bootCfg);

    // todo, move to custom logic with nmap\loadlibraryN
    if (appCfg.UseNativeLoader)
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), Resolver);
        NativeLibrary.SetDllImportResolver(typeof(LLVMSharp.Interop.LLVM).Assembly, Resolver);
    }
    
    IntPtr Resolver(string libname, Assembly assembly, DllImportSearchPath? search_path)
    {
        var path = appCfg.LibraryPath(libname);

        try
        {
            if (!NativeLibrary.TryLoad(path.ToString(), out var handle))
                Console.WriteLine($"[TryLoad] failed load '{libname}', path: '{path.ToString()}', pinvokeErr: {Marshal.GetLastPInvokeError()}, msg: {Marshal.GetLastPInvokeErrorMessage()}" +
                                  $"sysErr: {Marshal.GetLastSystemError()}, win32Err: {Marshal.GetLastWin32Error()}");
            return handle;
        }
        catch (Exception e)
        {
            Console.WriteLine($"failed load '{libname}', path: '{path.ToString()}', pinvokeErr: {Marshal.GetLastPInvokeError()}, msg: {Marshal.GetLastPInvokeErrorMessage()}" +
                              $"sysErr: {Marshal.GetLastSystemError()}, win32Err: {Marshal.GetLastWin32Error()}");
            return 0;
        }

        return 0;
    }

    VirtualMachine.static_init();

    var vm = VirtualMachine.Create("app", appCfg);
    var vault = vm->Vault;

    IshtarThreading.SetName($"ishtar::entry");

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
            vm->FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, "0x1 [module path is not passed]", vm->Frames->EntryPoint);
            return -1;
        }
        var entry = new FileInfo(args.First());
        if (!entry.Exists)
        {
            vm->FastFail(WNE.ASSEMBLY_COULD_NOT_LOAD, $"0x2 [{entry.FullName} is not found]", vm->Frames->EntryPoint);
            return -2;
        }
        vault.WorkDirectory = entry.Directory;
        resolver = vault.GetResolver();
        masterModule = IshtarAssembly.LoadFromFile(entry);
        resolver.AddSearchPath(entry.Directory);
    }


    var module = resolver.Resolve(masterModule);

    module->class_table->ForEach(x => x->init_vtable(x->Owner->vm));
    
    var entry_point = module->GetSpecialEntryPoint(vm->Config.EntryPoint, vm->Config.EntryPointClass);

    if (entry_point is null)
    {
        vm->FastFail(WNE.MISSING_METHOD, $"Entry point '{vm->Config.EntryPoint.ToString()}' in '{module->Name}' module is not defined.", vm->Frames->EntryPoint);
        return -280;
    }

    var args_ = stackalloc stackval[1];
    
    var frame = CallFrame.Create(entry_point, null);
    frame->args = args_;

    var watcher = Stopwatch.StartNew();

    vm->task_scheduler->start_threading(vm);
    vm->exec_method(frame);
    
    watcher.Stop();
    vm->trace.log($"Elapsed: {watcher.Elapsed}");

    vm->hasStopRequired = true;
    vm->thread_pool->Stop();
    frame->Dispose();
    vm->Dispose();
    return 0;
}
