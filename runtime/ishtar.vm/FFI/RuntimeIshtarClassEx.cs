namespace ishtar
{
    using vein.runtime;

    public static class RuntimeIshtarClassEx
    {
        public static RuntimeIshtarClass AsRuntimeClass(this VeinTypeCode code)
            => (RuntimeIshtarClass)code.AsClass();
    }
}
