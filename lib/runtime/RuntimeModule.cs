using System.Runtime.CompilerServices;
using insomnia.emit;

[assembly: InternalsVisibleTo("wc_test")]
[assembly: InternalsVisibleTo("wc")]
[assembly: InternalsVisibleTo("insomnia.runtime.transition")]
[assembly: InternalsVisibleTo("insomnia.common")]
[assembly: InternalsVisibleTo("wave.project")]


public static class RuntimeModule
{
    [ModuleInitializer]
    public static void Init() => WaveCore.Init();
}