namespace ishtar;

using vein.runtime;

[ExcludeFromCodeCoverage]
[CHeaderExport("ishtar.h")]
[CHeaderInclude("stdint.h")]
public static unsafe class NativeExports
{
    [UnmanagedCallersOnly(EntryPoint = "vm_init")]
    public static void vm_init()
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "execute_method")]
    public static void execute_method(CallFrame* frame)
    {
    }
    [UnmanagedCallersOnly(EntryPoint = "create_method")]
    public static void create_method(void* name, MethodFlags flags, RuntimeIshtarClass* returnType, RuntimeIshtarClass** args)
    {

    }
}
