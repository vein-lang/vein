namespace ishtar;

using io;
using runtime.gc;
using runtime.vin;
using vm.runtime;
using llmv;
using runtime;

public unsafe partial class VirtualMachine
{
    public static VirtualMachine Create(string name)
    {
        var vm = new VirtualMachine();

        BoehmGCLayout.Native.Load(vm.runtimeInfo);
        BoehmGCLayout.Native.GC_set_find_leak(true);
        BoehmGCLayout.Native.GC_init();
        BoehmGCLayout.Native.GC_allow_register_threads();

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
