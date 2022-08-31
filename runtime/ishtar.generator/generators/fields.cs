namespace ishtar;

using vein.runtime;
using vein.syntax;

public static partial class GeneratorExtension
{
    public static bool ContainsField(this VeinClass @class, IdentifierExpression id)
        => @class.FindField(id.ExpressionString) != null;
    public static VeinField ResolveField(this VeinClass @class, IdentifierExpression id)
        => @class.FindField(id.ExpressionString);
}
