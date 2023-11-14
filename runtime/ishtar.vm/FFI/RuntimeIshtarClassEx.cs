namespace ishtar
{
    using vein.runtime;

    public static class RuntimeIshtarClassEx
    {
        public static RuntimeIshtarClass AsRuntimeClass(this VeinTypeCode code, IshtarCore types)
            => (RuntimeIshtarClass)code.AsClass()(types);
    }
}
