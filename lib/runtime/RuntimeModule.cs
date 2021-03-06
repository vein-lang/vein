using System.Runtime.CompilerServices;
using wave.emit;

[assembly: InternalsVisibleTo("wc_test")]
[assembly: InternalsVisibleTo("wc")]
[assembly: InternalsVisibleTo("Insomnia.Runtime.Transition")]


public static class RuntimeModule
{
    [ModuleInitializer]
    public static void Init() => WaveCore.Init();
}