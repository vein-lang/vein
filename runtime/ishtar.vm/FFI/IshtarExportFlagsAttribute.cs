namespace ishtar
{
    using vein.runtime;

    [AttributeUsage(AttributeTargets.Method)]
    [ExcludeFromCodeCoverage]
    public class IshtarExportFlagsAttribute : Attribute
    {
        public IshtarExportFlagsAttribute(MethodFlags flags)
        {

        }
    }
}
