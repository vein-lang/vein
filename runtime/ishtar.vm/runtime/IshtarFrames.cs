namespace ishtar;

using vein.runtime;

public class IshtarFrames(VirtualMachine vm)
{
    public CallFrame ModuleLoaderFrame = new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".module")
    };

    public CallFrame VTableFrame(VeinClass clazz) => new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".type", clazz),
    };

    public CallFrame StaticCtor(VeinClass clazz) => new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".static_ctor", clazz),
    };

    public CallFrame EntryPoint = new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod("ishtar_entry")
    };

    public CallFrame Jit() => new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".jit")
    };

    public CallFrame GarbageCollector() => new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".gc")
    };

    public CallFrame NativeLoader() => new CallFrame(vm)
    {
        method = vm.DefineEmptySystemMethod(".ffi")
    };
}
