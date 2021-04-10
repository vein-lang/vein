namespace insomnia.compilation
{
    using emit;
    using wave.extensions;
    using syntax;

    internal static class WaveTypeCodeExtensions
    {
        public static bool CanImplicitlyCast(this WaveTypeCode code, NumericLiteralExpressionSyntax numeric)
        {
            if (code.IsCompatibleNumber(numeric.GetTypeCode()))
                return true;

            switch (code)
            {
                case WaveTypeCode.TYPE_I1:
                    return long.Parse(numeric.ExpressionString) is <= sbyte.MaxValue and >= sbyte.MinValue;
                case WaveTypeCode.TYPE_I2:
                    return long.Parse(numeric.ExpressionString) is <= short.MaxValue and >= short.MinValue;
                case WaveTypeCode.TYPE_I4:
                    return long.Parse(numeric.ExpressionString) is <= int.MaxValue and >= int.MinValue;

                case WaveTypeCode.TYPE_U1:
                    return ulong.Parse(numeric.ExpressionString) is <= byte.MaxValue and >= byte.MinValue;
                case WaveTypeCode.TYPE_U2:
                    return ulong.Parse(numeric.ExpressionString) is <= ushort.MaxValue and >= ushort.MinValue;
                case WaveTypeCode.TYPE_U4:
                    return ulong.Parse(numeric.ExpressionString) is <= uint.MaxValue and >= uint.MinValue;
            }

            return false;
        }
    }
}