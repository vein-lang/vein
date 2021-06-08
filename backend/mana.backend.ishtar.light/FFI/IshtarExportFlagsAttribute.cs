namespace ishtar
{
    using System;
    using mana.runtime;

    [AttributeUsage(AttributeTargets.Method)]
    public class IshtarExportFlagsAttribute : Attribute
    {
        public IshtarExportFlagsAttribute(MethodFlags flags)
        {

        }
    }
}
