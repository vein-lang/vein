// ReSharper disable MethodNameNotMeaningful
namespace ishtar;

using vein.runtime;

public unsafe class IshtarFrames(VirtualMachine vm)
{
    public CallFrame ModuleLoaderFrame = new(vm)
    {
        method = vm.DefineEmptySystemMethod("#module")
    };

    public CallFrame VTableFrame(RuntimeIshtarClass* clazz)
    {
        var method = clazz->FindMethod($"#type()");

        if (method is not null)
            return new CallFrame(vm) { method = method };

        return new CallFrame(vm)
        {
            method = clazz->DefineMethod($"#type()", clazz, MethodFlags.Special)
        };
    }

    public CallFrame StaticCtor(RuntimeIshtarClass* clazz)
    {
        var method = clazz->FindMethod($"#static_ctor()");

        if (method is not null)
            return new CallFrame(vm) { method = method };

        return new CallFrame(vm)
        {
            method = clazz->DefineMethod("#static_ctor()", null, MethodFlags.Special | MethodFlags.Static)
        };
    }


    public CallFrame EntryPoint = new(vm)
    {
        method = vm.DefineEmptySystemMethod("ishtar_entry")
    };

    public CallFrame Jit = new(vm)
    {
        method = vm.DefineEmptySystemMethod("#jit")
    };

    public CallFrame GarbageCollector = new(vm)
    {
        method = vm.DefineEmptySystemMethod("#gc")
    };

    public CallFrame NativeLoader = new(vm)
    {
        method = vm.DefineEmptySystemMethod("#ffi")
    };
}
