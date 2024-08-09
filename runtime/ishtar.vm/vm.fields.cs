namespace ishtar;

using io;
using runtime.gc;
using runtime.vin;
using runtime;
using vm.runtime;
using llmv;

[CTypeExport("vm_t")]
public unsafe struct VirtualMachineRef
{
    public InternedString* Name;

    public IshtarFrames* Frames;
    internal IshtarTrace trace;
    internal LLVMContext Jitter;
    public IshtarTypes* Types;
    public IshtarThreading threading;
    public TaskScheduler* task_scheduler;
    internal RuntimeIshtarModule* InternalModule;
    internal RuntimeIshtarClass* InternalClass;
}

public unsafe partial class VirtualMachine : IDisposable
{
    public readonly RuntimeInfo runtimeInfo = new();

    public VirtualMachineRef* @ref;

    public volatile NativeException CurrentException;
    public volatile IWatchDog watcher;
    public volatile AppVault Vault;
    public volatile IshtarGC GC;
    public volatile ForeignFunctionInterface FFI;
    public volatile IshtarJIT Jit;
    public volatile NativeStorage NativeStorage;
    public AppConfig Config;

    public IshtarFrames* Frames => @ref->Frames;
    internal IshtarTrace trace => @ref->trace;
    internal LLVMContext Jitter => @ref->Jitter;
    public IshtarTypes* Types => @ref->Types;
    public IshtarThreading threading => @ref->threading;
    public TaskScheduler* task_scheduler => @ref->task_scheduler;
    internal RuntimeIshtarModule* InternalModule => @ref->InternalModule;
    internal RuntimeIshtarClass* InternalClass => @ref->InternalClass;
}
