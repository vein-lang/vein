namespace ishtar;

using vein.runtime;

public static class IshtarFrames
{
    public static CallFrame ModuleLoaderFrame = new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod(".module")
    };
    public static CallFrame VTableFrame(VeinClass clazz) => new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod(".type", clazz),
    };

    public static CallFrame StaticCtor(VeinClass clazz) => new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod(".static_ctor", clazz),
    };
    public static CallFrame EntryPoint = new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod("ishtar_entry")
    };
}
