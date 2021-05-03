namespace ishtar
{
    using System;
    using wave.runtime;

    [AttributeUsage(AttributeTargets.Method)]
    public class IshtarExportFlagsAttribute : Attribute
    {
        public IshtarExportFlagsAttribute(MethodFlags flags)
        {
            
        } 
    }
}