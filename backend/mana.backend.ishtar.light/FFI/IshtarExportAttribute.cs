namespace ishtar
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class IshtarExportAttribute : Attribute
    {
        public IshtarExportAttribute(int argLen, string name)
        {

        }
    }
}