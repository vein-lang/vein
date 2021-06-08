namespace mana.syntax
{
    using System;
    using System.Linq.Expressions;
    using runtime;

    public static class ManaExpression
    {
        public static ExpressionSyntax Const<T>(ManaTypeCode code, T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            var str = value.ToString() ?? throw new ArgumentNullException(nameof(value));
            switch (code)
            {
                case ManaTypeCode.TYPE_NONE:
                case ManaTypeCode.TYPE_VOID:
                case ManaTypeCode.TYPE_OBJECT:
                case ManaTypeCode.TYPE_CHAR:
                case ManaTypeCode.TYPE_CLASS:
                case ManaTypeCode.TYPE_ARRAY:
                    throw new NotImplementedException();
                case ManaTypeCode.TYPE_BOOLEAN:
                    return new BoolLiteralExpressionSyntax(str.ToLowerInvariant());
                case ManaTypeCode.TYPE_I1:
                    return new SByteLiteralExpressionSyntax(sbyte.Parse(str));
                case ManaTypeCode.TYPE_U1:
                    return new ByteLiteralExpressionSyntax(byte.Parse(str));
                case ManaTypeCode.TYPE_I2:
                    return new Int16LiteralExpressionSyntax(short.Parse(str));
                case ManaTypeCode.TYPE_U2:
                    return new UInt16LiteralExpressionSyntax(ushort.Parse(str));
                case ManaTypeCode.TYPE_I4:
                    return new Int32LiteralExpressionSyntax(int.Parse(str));
                case ManaTypeCode.TYPE_U4:
                    return new UInt32LiteralExpressionSyntax(uint.Parse(str));
                case ManaTypeCode.TYPE_I8:
                    return new Int64LiteralExpressionSyntax(long.Parse(str));
                case ManaTypeCode.TYPE_U8:
                    return new UInt64LiteralExpressionSyntax(ulong.Parse(str));
                case ManaTypeCode.TYPE_R2:
                    return new HalfLiteralExpressionSyntax(float.Parse(str));
                case ManaTypeCode.TYPE_R4:
                    return new SingleLiteralExpressionSyntax(float.Parse(str));
                case ManaTypeCode.TYPE_R8:
                    return new DoubleLiteralExpressionSyntax(double.Parse(str));
                case ManaTypeCode.TYPE_R16:
                    return new DecimalLiteralExpressionSyntax(decimal.Parse(str));
                case ManaTypeCode.TYPE_STRING:
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
