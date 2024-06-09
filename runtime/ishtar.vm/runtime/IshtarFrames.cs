// ReSharper disable MethodNameNotMeaningful
namespace ishtar;

using vein.runtime;

public unsafe struct IshtarFrames
{
    public IshtarFrames(VirtualMachine vm)
    {
        EntryPoint = CallFrame.Create(vm.DefineEmptySystemMethod("ishtar_entry"), null);
        ModuleLoaderFrame = CallFrame.Create(vm.DefineEmptySystemMethod("#module"), EntryPoint);
        Jit = CallFrame.Create(vm.DefineEmptySystemMethod("#jit"), EntryPoint);
        GarbageCollector = CallFrame.Create(vm.DefineEmptySystemMethod("#gc"), EntryPoint);
        NativeLoader = CallFrame.Create(vm.DefineEmptySystemMethod("#ffi"), EntryPoint);
    }


    public readonly CallFrame* ModuleLoaderFrame;
    public readonly CallFrame* EntryPoint;
    public readonly CallFrame* Jit;
    public readonly CallFrame* GarbageCollector;
    public readonly CallFrame* NativeLoader;
}
