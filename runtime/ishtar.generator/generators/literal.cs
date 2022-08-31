namespace ishtar;

using System;
using System.Text.RegularExpressions;
using emit;
using vein.syntax;

public static class G_Literal
{
    public static ILGenerator EmitNumericLiteral(this ILGenerator generator, NumericLiteralExpressionSyntax literal)
    {
        switch (literal)
        {
            case Int16LiteralExpressionSyntax i16:
                if (i16 is { Value: 0 }) return generator.Emit(OpCodes.LDC_I2_0);
                if (i16 is { Value: 1 }) return generator.Emit(OpCodes.LDC_I2_1);
                if (i16 is { Value: 2 }) return generator.Emit(OpCodes.LDC_I2_2);
                if (i16 is { Value: 3 }) return generator.Emit(OpCodes.LDC_I2_3);
                if (i16 is { Value: 4 }) return generator.Emit(OpCodes.LDC_I2_4);
                if (i16 is { Value: 5 }) return generator.Emit(OpCodes.LDC_I2_5);
                if (i16.Value is > 5 or < 0)
                    return generator.Emit(OpCodes.LDC_I2_S, i16.Value);
                goto default;
            case Int32LiteralExpressionSyntax i32:
                if (i32 is { Value: 0 }) return generator.Emit(OpCodes.LDC_I4_0);
                if (i32 is { Value: 1 }) return generator.Emit(OpCodes.LDC_I4_1);
                if (i32 is { Value: 2 }) return generator.Emit(OpCodes.LDC_I4_2);
                if (i32 is { Value: 3 }) return generator.Emit(OpCodes.LDC_I4_3);
                if (i32 is { Value: 4 }) return generator.Emit(OpCodes.LDC_I4_4);
                if (i32 is { Value: 5 }) return generator.Emit(OpCodes.LDC_I4_5);
                if (i32.Value is > 5 or < 0)
                    return generator.Emit(OpCodes.LDC_I4_S, i32.Value);
                goto default;
            case Int64LiteralExpressionSyntax i64:
                if (i64 is { Value: 0 }) return generator.Emit(OpCodes.LDC_I8_0);
                if (i64 is { Value: 1 }) return generator.Emit(OpCodes.LDC_I8_1);
                if (i64 is { Value: 2 }) return generator.Emit(OpCodes.LDC_I8_2);
                if (i64 is { Value: 3 }) return generator.Emit(OpCodes.LDC_I8_3);
                if (i64 is { Value: 4 }) return generator.Emit(OpCodes.LDC_I8_4);
                if (i64 is { Value: 5 }) return generator.Emit(OpCodes.LDC_I8_5);
                if (i64.Value is > 5 or < 0)
                    return generator.Emit(OpCodes.LDC_I8_S, i64.Value);
                goto default;
            case DecimalLiteralExpressionSyntax f128:
                return generator.Emit(OpCodes.LDC_F16, f128.Value);
            case DoubleLiteralExpressionSyntax f64:
                return generator.Emit(OpCodes.LDC_F8, f64.Value);
            case SingleLiteralExpressionSyntax f32:
                return generator.Emit(OpCodes.LDC_F4, f32.Value);
            case HalfLiteralExpressionSyntax h32:
                return generator.Emit(OpCodes.LDC_F2, h32.Value);
            case NegativeInfinityLiteralExpressionSyntax:
                return generator.Emit(OpCodes.LDC_F4, float.NegativeInfinity);
            case InfinityLiteralExpressionSyntax:
                return generator.Emit(OpCodes.LDC_F4, float.PositiveInfinity);
            case NaNLiteralExpressionSyntax:
                return generator.Emit(OpCodes.LDC_F4, float.NaN);
            default:
                throw new NotImplementedException();
        }
    }

    public static ILGenerator EmitLiteral(this ILGenerator generator, LiteralExpressionSyntax literal)
    {
        if (literal is NumericLiteralExpressionSyntax numeric)
            generator.EmitNumericLiteral(numeric);
        else if (literal is StringLiteralExpressionSyntax stringLiteral)
            generator.Emit(OpCodes.LDC_STR, UnEscapeSymbols(stringLiteral.Value));
        else if (literal is BoolLiteralExpressionSyntax boolLiteral)
            generator.Emit(boolLiteral.Value ? OpCodes.LDC_I2_1 : OpCodes.LDC_I2_0);
        else if (literal is NullLiteralExpressionSyntax)
            generator.Emit(OpCodes.LDNULL);
        return generator;
    }

    private static string UnEscapeSymbols(string str)
        => Regex.Unescape(str);
}
