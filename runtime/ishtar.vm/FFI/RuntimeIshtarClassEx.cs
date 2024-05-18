namespace ishtar
{
    using vein.runtime;

    public static unsafe class RuntimeIshtarClassEx
    {
        public static RuntimeIshtarClass* AsRuntimeClass(this VeinTypeCode code, IshtarTypes* types)
            => types->ByTypeCode(code);
    }
}
