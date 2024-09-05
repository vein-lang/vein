namespace ishtar;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public class IshtarExportAttribute : Attribute
{
    public IshtarExportAttribute(int argLen, string name)
    {

    }
}
