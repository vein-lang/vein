namespace wc_test
{
    using System;
    using System.Runtime.Serialization;
    using Sprache;
    using ishtar;
    using mana.runtime;
    using mana.syntax;
    using Xunit;

    public class compatible_number_test
    {
        [Theory]
        [InlineData(ManaTypeCode.TYPE_R4, ManaTypeCode.TYPE_R8)]
        [InlineData(ManaTypeCode.TYPE_R4, ManaTypeCode.TYPE_R16)]
        [InlineData(ManaTypeCode.TYPE_R8, ManaTypeCode.TYPE_R16)]
        [InlineData(ManaTypeCode.TYPE_R16, ManaTypeCode.TYPE_R4)]
        [InlineData(ManaTypeCode.TYPE_R16, ManaTypeCode.TYPE_R8)]
        [InlineData(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_U4)]
        [InlineData(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_U8)]
        [InlineData(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_I8)]
        [InlineData(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_I4)]
        public void CompatibleFalse(ManaTypeCode variable, ManaTypeCode value)
            => Assert.False(variable.IsCompatibleNumber(value));

        [Theory]
        [InlineData(ManaTypeCode.TYPE_I4, ManaTypeCode.TYPE_U1)]
        [InlineData(ManaTypeCode.TYPE_I4, ManaTypeCode.TYPE_I1)]
        public void CompatibleTrue(ManaTypeCode variable, ManaTypeCode value)
            => Assert.True(variable.IsCompatibleNumber(value));

        [Theory]
        [InlineData(typeof(SByteLiteralExpressionSyntax), ManaTypeCode.TYPE_I1)]
        [InlineData(typeof(Int16LiteralExpressionSyntax), ManaTypeCode.TYPE_I2)]
        [InlineData(typeof(Int32LiteralExpressionSyntax), ManaTypeCode.TYPE_I4)]
        [InlineData(typeof(Int64LiteralExpressionSyntax), ManaTypeCode.TYPE_I8)]
        [InlineData(typeof(ByteLiteralExpressionSyntax), ManaTypeCode.TYPE_U1)]
        [InlineData(typeof(UInt16LiteralExpressionSyntax), ManaTypeCode.TYPE_U2)]
        [InlineData(typeof(UInt32LiteralExpressionSyntax), ManaTypeCode.TYPE_U4)]
        [InlineData(typeof(UInt64LiteralExpressionSyntax), ManaTypeCode.TYPE_U8)]
        [InlineData(typeof(SingleLiteralExpressionSyntax), ManaTypeCode.TYPE_R4)]
        [InlineData(typeof(DoubleLiteralExpressionSyntax), ManaTypeCode.TYPE_R8)]
        [InlineData(typeof(DecimalLiteralExpressionSyntax), ManaTypeCode.TYPE_R16)]
        public void TypeCodeValidTest(Type t, ManaTypeCode code)
            => Assert.Equal(code, CreateExpressionByType(t).GetTypeCode());

        [Theory]
        [InlineData(ManaTypeCode.TYPE_U1, (byte)22)]
        [InlineData(ManaTypeCode.TYPE_I1, (sbyte)-22)]
        [InlineData(ManaTypeCode.TYPE_I2, short.MaxValue / 2)]
        [InlineData(ManaTypeCode.TYPE_I4, int.MaxValue / 2)]
        [InlineData(ManaTypeCode.TYPE_I8, long.MaxValue / 2)]
        [InlineData(ManaTypeCode.TYPE_I8, long.MinValue / 2)]
        [InlineData(ManaTypeCode.TYPE_I4, int.MinValue / 2)]
        [InlineData(ManaTypeCode.TYPE_I2, short.MinValue / 2)]
        [InlineData(ManaTypeCode.TYPE_U2, ushort.MaxValue)]
        [InlineData(ManaTypeCode.TYPE_U4, uint.MaxValue)]
        [InlineData(ManaTypeCode.TYPE_U8, ulong.MaxValue)]
        public void DetectTypeCode(ManaTypeCode code, object value)
        {
            var str = value.ToString();
            var result =
                FieldDeclaratorSyntax.RedefineIntegerExpression(new UndefinedIntegerNumericLiteral(str)
                    .SetPos(new Position(0, 0, 0), 0), false);

            Assert.Equal(code, result.GetTypeCode());
        }

        private NumericLiteralExpressionSyntax CreateExpressionByType(Type t) =>
            (NumericLiteralExpressionSyntax)FormatterServices
                .GetUninitializedObject(t);
    }
}