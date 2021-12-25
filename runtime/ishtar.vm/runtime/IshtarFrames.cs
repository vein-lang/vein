namespace ishtar;

using vein.runtime;

public static class IshtarFrames
{
    public static CallFrame ModuleLoaderFrame = new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod("loader")
    };
    public static CallFrame VTableFrame(VeinClass clazz) => new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod("loader", clazz),
    };
    public static CallFrame EntryPoint = new CallFrame()
    {
        method = RuntimeIshtarMethod.DefineEmptySystemMethod("ishtar_entry")
    };
}
