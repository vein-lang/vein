// ReSharper disable MethodNameNotMeaningful
namespace ishtar;

public unsafe class IshtarFrames(VirtualMachine vm)
{
    public CallFrame ModuleLoaderFrame = new(vm)
    {
        method = vm.DefineEmptySystemMethod(".module")
    };

    public CallFrame VTableFrame(RuntimeIshtarClass* clazz) => new(vm)
    {
        method = vm.DefineEmptySystemMethod(".type", clazz),
    };

    public CallFrame StaticCtor(RuntimeIshtarClass* clazz) => new(vm)
    {
        method = vm.DefineEmptySystemMethod(".static_ctor", clazz),
    };

    public CallFrame EntryPoint = new(vm)
    {
        method = vm.DefineEmptySystemMethod("ishtar_entry")
    };

    public CallFrame Jit() => new(vm)
    {
        method = vm.DefineEmptySystemMethod(".jit")
    };

    public CallFrame GarbageCollector() => new(vm)
    {
        method = vm.DefineEmptySystemMethod(".gc")
    };

    public CallFrame NativeLoader() => new(vm)
    {
        method = vm.DefineEmptySystemMethod(".ffi")
    };
}
