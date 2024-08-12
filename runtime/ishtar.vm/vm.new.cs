namespace ishtar;

using io;
using runtime.gc;
using runtime.vin;
using llmv;
using runtime;
using static runtime.gc.BoehmGCLayout.Native;

public unsafe partial struct VirtualMachine(VirtualMachine* self)
{
    public static bool hasInited;
    public static void static_init()
    {
        if (hasInited)
            throw new NotSupportedException();
        GC_set_find_leak(true);
        GC_set_all_interior_pointers(true);
        GC_set_finalizer_notifier(on_gc_finalization);
        GC_init();
        GC_allow_register_threads();
        libuv_gc_allocator.install();
        hasInited = true;
    }

    public static VirtualMachine* Create(string name)
    {
        var vm = IshtarGC.AllocateImmortal<VirtualMachine>(null);
        *vm = new VirtualMachine(vm);
        vm->Name = StringStorage.Intern(name, vm);
        var vault = AppVault.Create(vm, name);
        

        vm->boot_cfg = readBootCfg();

        vm->Config = new AppConfig(vm->@ref->boot_cfg);
        vm->trace = new IshtarTrace();

        vm->trace.Setup();

        vm->Types = IshtarTypes.Create(vm->Vault);
        vm->gc = IshtarGC.Create(vm);

        vm->InternalModule = vm->Vault.DefineModule("$ishtar$");

        vm->@ref->InternalClass = vm->InternalModule->DefineClass(RuntimeQualityTypeName.New("global", "sys", "ishtar", vm->@ref->InternalModule),
            vm->Types->ObjectClass);

        vm->Frames = IshtarFrames.Create(vm);
        vm->watcher = new IshtarWatchDog(vm);

        vm->@ref->Jitter = new LLVMContext();

        vm->@ref->threading = new IshtarThreading(vm);

        vm->@ref->task_scheduler = vm->threading.CreateScheduler(vm);

        vault.PostInit();

        return vm;
    }

    private static void on_gc_finalization()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("on_gc_finalization");
        Console.ResetColor();
    }

    public void Dispose()
    {
        task_scheduler->Dispose();
        InternalModule->Dispose();
        IshtarGC.FreeImmortalRoot(InternalModule);

        gc->Dispose();
        Vault.Dispose();
        StringStorage.Dispose();
    }
}
