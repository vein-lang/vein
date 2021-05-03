namespace ishtar
{
    using wave.runtime;

    public static class RuntimeIshtarClassEx
    {
        public static RuntimeIshtarClass AsRuntimeClass(this WaveTypeCode code) 
            => (RuntimeIshtarClass)code.AsClass();
    }
}