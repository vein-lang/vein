using System.Runtime.CompilerServices;
using vein.runtime;

[assembly: InternalsVisibleTo("wc_test")]
[assembly: InternalsVisibleTo("wc")]
[assembly: InternalsVisibleTo("insomnia.runtime.transition")]
[assembly: InternalsVisibleTo("insomnia.common")]
[assembly: InternalsVisibleTo("mana.project")]


public static class RuntimeModule
{
    [ModuleInitializer]
    public static void Init() => VeinCore.Init();
}
