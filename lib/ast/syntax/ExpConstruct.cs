namespace vein.syntax
{
    using System;
    using System.Linq.Expressions;
    using vein.runtime;

    public static class ManaExpression
    {
        public static ExpressionSyntax Const<T>(VeinTypeCode code, T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            var str = value.ToString() ?? throw new ArgumentNullException(nameof(value));
            switch (code)
            {
                case VeinTypeCode.TYPE_NONE:
                case VeinTypeCode.TYPE_VOID:
                case VeinTypeCode.TYPE_OBJECT:
                case VeinTypeCode.TYPE_CHAR:
                case VeinTypeCode.TYPE_CLASS:
                case VeinTypeCode.TYPE_ARRAY:
                    throw new NotImplementedException();
                case VeinTypeCode.TYPE_BOOLEAN:
                    return new BoolLiteralExpressionSyntax(str.ToLowerInvariant());
                case VeinTypeCode.TYPE_I1:
                    return new SByteLiteralExpressionSyntax(sbyte.Parse(str));
                case VeinTypeCode.TYPE_U1:
                    return new ByteLiteralExpressionSyntax(byte.Parse(str));
                case VeinTypeCode.TYPE_I2:
                    return new Int16LiteralExpressionSyntax(short.Parse(str));
                case VeinTypeCode.TYPE_U2:
                    return new UInt16LiteralExpressionSyntax(ushort.Parse(str));
                case VeinTypeCode.TYPE_I4:
                    return new Int32LiteralExpressionSyntax(int.Parse(str));
                case VeinTypeCode.TYPE_U4:
                    return new UInt32LiteralExpressionSyntax(uint.Parse(str));
                case VeinTypeCode.TYPE_I8:
                    return new Int64LiteralExpressionSyntax(long.Parse(str));
                case VeinTypeCode.TYPE_U8:
                    return new UInt64LiteralExpressionSyntax(ulong.Parse(str));
                case VeinTypeCode.TYPE_R2:
                    return new HalfLiteralExpressionSyntax(float.Parse(str));
                case VeinTypeCode.TYPE_R4:
                    return new SingleLiteralExpressionSyntax(float.Parse(str));
                case VeinTypeCode.TYPE_R8:
                    return new DoubleLiteralExpressionSyntax(double.Parse(str));
                case VeinTypeCode.TYPE_R16:
                    return new DecimalLiteralExpressionSyntax(decimal.Parse(str));
                case VeinTypeCode.TYPE_STRING:
                    return new StringLiteralExpressionSyntax(str);
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }


        public static BinaryExpressionSyntax Add(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.Add);
        public static BinaryExpressionSyntax Sub(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.Subtract);
        public static BinaryExpressionSyntax Div(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.Divide);
        public static BinaryExpressionSyntax Mul(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.Multiply);

        public static BinaryExpressionSyntax Equal(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.Equal);

        public static BinaryExpressionSyntax AndAlso(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2, ExpressionType.AndAlso);

        public static UnaryExpressionSyntax Negate(ExpressionSyntax exp1) =>
            new() { Operand = exp1, OperatorType = ExpressionType.Negate };

        public static CoalescingExpressionSyntax Coalescing(ExpressionSyntax exp1, ExpressionSyntax exp2)
            => new(exp1, exp2);
    }
}
