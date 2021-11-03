using System.Runtime.CompilerServices;
using vein.runtime;
public static class RuntimeModule
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Init() => VeinCore.Init();
}
