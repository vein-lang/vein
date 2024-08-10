namespace ishtar;

using io;
using runtime.gc;
using runtime.vin;
using vm.runtime;
using llmv;
using runtime;
using static runtime.gc.BoehmGCLayout.Native;

public unsafe partial class VirtualMachine
{
    public static VirtualMachine Create(string name)
    {
        var vm = new VirtualMachine();

        Load(vm.runtimeInfo);
        GC_set_find_leak(true);
        GC_set_all_interior_pointers(true);
        GC_set_finalizer_notifier(on_gc_finalization);
        GC_init();
        GC_allow_register_threads();
        libuv_gc_allocator.install();

        vm.@ref = IshtarGC.AllocateImmortal<VirtualMachineRef>(null);

        vm.Jit = new IshtarJIT(vm);
        vm.Config = new AppConfig();
        vm.Vault = new AppVault(vm, name);
        vm.@ref->trace = new IshtarTrace();

        vm.trace.Setup();

        vm.@ref->Types = IshtarTypes.Create(vm.Vault);
        vm.GC = new IshtarGC(vm);

        vm.@ref->InternalModule = vm.Vault.DefineModule("$ishtar$");

        vm.@ref->InternalClass = vm.InternalModule->DefineClass(RuntimeQualityTypeName.New("global", "sys", "ishtar", vm.@ref->InternalModule),
            vm.Types->ObjectClass);

        vm.@ref->Frames = IshtarFrames.Create(vm);
        vm.watcher = new DefaultWatchDog(vm);

        vm.NativeStorage = new NativeStorage(vm);
        vm.GC.init();

        vm.FFI = new ForeignFunctionInterface(vm);
        vm.@ref->Jitter = new LLVMContext();

        vm.@ref->threading = new IshtarThreading(vm);

        vm.@ref->task_scheduler = vm.threading.CreateScheduler(vm);

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

        GC.Dispose();
        Vault.Dispose();
        StringStorage.Dispose();
    }
}
