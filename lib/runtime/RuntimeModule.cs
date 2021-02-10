
using System.Runtime.CompilerServices;
using wave.emit;

[assembly: InternalsVisibleTo("wc_test")]
public static class RuntimeModule
{
    [ModuleInitializer]
    public static void Init() => WaveCore.Init();
}