namespace wave
{
    using System.Runtime.CompilerServices;
    using emit;

    public static class RuntimeModule
    {
        [ModuleInitializer]
        public static void Init()
        {
            WaveCore.Init();
        }
    }
}