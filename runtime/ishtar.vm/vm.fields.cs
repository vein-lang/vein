namespace ishtar;

using io;
using runtime.gc;
using runtime.vin;
using runtime;
using llmv;
using ishtar.runtime.io.ini;
using ishtar.runtime.io;

[CTypeExport("vm_t")]
public unsafe partial struct VirtualMachine : IDisposable
{
    public readonly RuntimeInfo runtimeInfo = new();
    public readonly VirtualMachine* @ref = self;

    public AppVault Vault => AppVault.GetVault(@ref);
    public ForeignFunctionInterface FFI => Vault.FFI;
    public NativeStorage NativeStorage => Vault.NativeStorage;

    public InternedString* Name;

    public IshtarFrames* Frames;
    internal IshtarTrace trace;
    internal LLVMContext Jitter;
    public IshtarTypes* Types;
    public IshtarThreading threading;
    public TaskScheduler* task_scheduler;
    public IshtarThreadPool* thread_pool;
    internal RuntimeIshtarModule* InternalModule;
    internal RuntimeIshtarClass* InternalClass;
    public IniRoot* boot_cfg;
    public AppConfig Config;
    public IshtarMasterFault* currentFault;
    public IshtarWatchDog watcher;
    public IshtarGC* gc;
    public bool hasStopRequired;
}
