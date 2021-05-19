using System.Runtime.CompilerServices;
using mana.runtime;

[assembly: InternalsVisibleTo("wc_test")]
[assembly: InternalsVisibleTo("wc")]
[assembly: InternalsVisibleTo("insomnia.runtime.transition")]
[assembly: InternalsVisibleTo("insomnia.common")]
[assembly: InternalsVisibleTo("mana.project")]


public static class RuntimeModule
{
    [ModuleInitializer]
    public static void Init() => ManaCore.Init();
}