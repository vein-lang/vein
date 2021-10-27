using System.Runtime.CompilerServices;
using vein.runtime;

[assembly: InternalsVisibleTo("wc_test")]
[assembly: InternalsVisibleTo("wc")]
[assembly: InternalsVisibleTo("insomnia.runtime.transition")]
[assembly: InternalsVisibleTo("insomnia.common")]
[assembly: InternalsVisibleTo("mana.project")]


public static class RuntimeModule
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Init() => VeinCore.Init();
}
