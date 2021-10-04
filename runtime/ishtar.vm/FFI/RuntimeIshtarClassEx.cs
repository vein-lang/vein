namespace ishtar
{
    using mana.runtime;

    public static class RuntimeIshtarClassEx
    {
        public static RuntimeIshtarClass AsRuntimeClass(this ManaTypeCode code)
            => (RuntimeIshtarClass)code.AsClass();
    }
}
