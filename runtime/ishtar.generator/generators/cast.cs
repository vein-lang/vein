namespace ishtar;

using vein.runtime;
using vein.syntax;

public static partial class GeneratorExtension
{
    public static bool CanImplicitlyCast(this VeinTypeCode code, NumericLiteralExpressionSyntax numeric)
    {
        if (code.IsCompatibleNumber(numeric.GetTypeCode()))
            return true;

        return code switch
        {
            VeinTypeCode.TYPE_I1 => long.Parse(numeric.ExpressionString) is <= sbyte.MaxValue and >= sbyte.MinValue,
            VeinTypeCode.TYPE_I2 => long.Parse(numeric.ExpressionString) is <= short.MaxValue and >= short.MinValue,
            VeinTypeCode.TYPE_I4 => long.Parse(numeric.ExpressionString) is <= int.MaxValue and >= int.MinValue,
            VeinTypeCode.TYPE_U1 => ulong.Parse(numeric.ExpressionString) is <= byte.MaxValue and >= byte.MinValue,
            VeinTypeCode.TYPE_U2 => ulong.Parse(numeric.ExpressionString) is <= ushort.MaxValue and >= ushort.MinValue,
            VeinTypeCode.TYPE_U4 => ulong.Parse(numeric.ExpressionString) is <= uint.MaxValue and >= uint.MinValue,
            _ => false
        };
    }
}
