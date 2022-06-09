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
            return code switch
            {
                VeinTypeCode.TYPE_NONE or VeinTypeCode.TYPE_VOID or VeinTypeCode.TYPE_OBJECT or VeinTypeCode.TYPE_CHAR or VeinTypeCode.TYPE_CLASS or VeinTypeCode.TYPE_ARRAY => throw new NotImplementedException(),
                VeinTypeCode.TYPE_BOOLEAN => new BoolLiteralExpressionSyntax(str.ToLowerInvariant()),
                VeinTypeCode.TYPE_I1 => new SByteLiteralExpressionSyntax(sbyte.Parse(str)),
                VeinTypeCode.TYPE_U1 => new ByteLiteralExpressionSyntax(byte.Parse(str)),
                VeinTypeCode.TYPE_I2 => new Int16LiteralExpressionSyntax(short.Parse(str)),
                VeinTypeCode.TYPE_U2 => new UInt16LiteralExpressionSyntax(ushort.Parse(str)),
                VeinTypeCode.TYPE_I4 => new Int32LiteralExpressionSyntax(int.Parse(str)),
                VeinTypeCode.TYPE_U4 => new UInt32LiteralExpressionSyntax(uint.Parse(str)),
                VeinTypeCode.TYPE_I8 => new Int64LiteralExpressionSyntax(long.Parse(str)),
                VeinTypeCode.TYPE_U8 => new UInt64LiteralExpressionSyntax(ulong.Parse(str)),
                VeinTypeCode.TYPE_R2 => new HalfLiteralExpressionSyntax(float.Parse(str)),
                VeinTypeCode.TYPE_R4 => new SingleLiteralExpressionSyntax(float.Parse(str)),
                VeinTypeCode.TYPE_R8 => new DoubleLiteralExpressionSyntax(double.Parse(str)),
                VeinTypeCode.TYPE_R16 => new DecimalLiteralExpressionSyntax(decimal.Parse(str)),
                VeinTypeCode.TYPE_STRING => new StringLiteralExpressionSyntax(str),
                _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
            };
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
