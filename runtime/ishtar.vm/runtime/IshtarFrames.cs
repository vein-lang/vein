// ReSharper disable MethodNameNotMeaningful
namespace ishtar;

using runtime.gc;
using vein.runtime;

[CTypeExport("ishtar_frames_t")]
public unsafe struct IshtarFrames
{
    private IshtarFrames(VirtualMachine* vm)
    {
        EntryPoint = CallFrame.Create(vm->DefineEmptySystemMethod("ishtar_entry"), null);
        ModuleLoaderFrame = CallFrame.Create(vm->DefineEmptySystemMethod("#module"), EntryPoint);
        Jit = CallFrame.Create(vm->DefineEmptySystemMethod("#jit"), EntryPoint);
        GarbageCollector = CallFrame.Create(vm->DefineEmptySystemMethod("#gc"), EntryPoint);
        NativeLoader = CallFrame.Create(vm->DefineEmptySystemMethod("#ffi"), EntryPoint);
        ThreadScheduler = CallFrame.Create(vm->DefineEmptySystemMethod("#scheduler"), EntryPoint);
    }

    public static IshtarFrames* Create(VirtualMachine* vm)
    {
        var result = IshtarGC.AllocateImmortal<IshtarFrames>(vm);

        *result = new IshtarFrames(vm);

        return result;
    }


    public readonly CallFrame* ModuleLoaderFrame;
    public readonly CallFrame* EntryPoint;
    public readonly CallFrame* Jit;
    public readonly CallFrame* GarbageCollector;
    public readonly CallFrame* NativeLoader;
    public readonly CallFrame* ThreadScheduler;
}
