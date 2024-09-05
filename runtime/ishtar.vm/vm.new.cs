namespace ishtar;

using io;
using runtime.gc;
using llmv;
using runtime;
using runtime.io;
using static runtime.gc.BoehmGCLayout.Native;

public unsafe partial struct VirtualMachine(VirtualMachine* self)
{
    private static bool hasInited;
    public static void static_init()
    {
        if (hasInited)
            throw new NotSupportedException();
        using var tag = Profiler.Begin("vm:init");
        GC_set_find_leak(true);
        GC_set_all_interior_pointers(true);
        GC_set_finalizer_notifier(on_gc_finalization);
        GC_init();
        GC_allow_register_threads();
        hasInited = true;
    }

    public static VirtualMachine* Create(string name, AppConfig appCfg)
    {
        using var tag = Profiler.Begin("vm:create");
        var vm = IshtarGC.AllocateImmortal<VirtualMachine>(null);
        *vm = new VirtualMachine(vm);
        vm->Name = StringStorage.Intern(name, vm);
        var vault = AppVault.Create(vm, name);
        
        vm->boot_cfg = appCfg.rootCfg;

        vm->Config = appCfg;
        vm->trace = new IshtarTrace(vm);

        vm->trace.Setup();

        vm->Types = IshtarTypes.Create(vm->Vault);
        vm->gc = IshtarGC.Create(vm);

        vm->InternalModule = vm->Vault.DefineModule("$ishtar$");

        vm->@ref->InternalClass = vm->InternalModule->DefineClass(RuntimeQualityTypeName.New("global", "sys", "ishtar", vm->@ref->InternalModule),
            vm->Types->ObjectClass);

        vm->Frames = IshtarFrames.Create(vm);
        vm->watcher = new IshtarWatchDog(vm);

        vm->@ref->Jitter = new LLVMContext(vm);

        vm->@ref->threading = new IshtarThreading(vm);

        vm->@ref->task_scheduler = vm->threading.CreateScheduler(vm);

        vm->@ref->thread_pool = IshtarThreadPool.Create(vm);
        
        vault.PostInit();
        return vm;
    }

    private static void on_gc_finalization() => Console.WriteLine("\u001b[31mon_gc_finalization\u001b[0m");

    public void Dispose()
    {
        using var tag = Profiler.Begin("vm:dispose");
        task_scheduler->Dispose();
        InternalModule->Dispose();
        IshtarGC.FreeImmortalRoot(InternalModule);

        gc->Dispose();
        Vault.Dispose();
        StringStorage.Dispose();
        tag.Complete();
    }
}
