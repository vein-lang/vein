namespace wc_test
{
    using System;
    using System.Runtime.Serialization;
    using Sprache;
    using insomnia.emit;
    using insomnia.extensions;
    using insomnia.syntax;
    using Xunit;

    public class compatible_number_test
    {
        [Theory]
        [InlineData(WaveTypeCode.TYPE_R4, WaveTypeCode.TYPE_R8)]
        [InlineData(WaveTypeCode.TYPE_R4, WaveTypeCode.TYPE_R16)]
        [InlineData(WaveTypeCode.TYPE_R8, WaveTypeCode.TYPE_R16)]
        [InlineData(WaveTypeCode.TYPE_R16, WaveTypeCode.TYPE_R4)]
        [InlineData(WaveTypeCode.TYPE_R16, WaveTypeCode.TYPE_R8)]
        [InlineData(WaveTypeCode.TYPE_U2, WaveTypeCode.TYPE_U4)]
        [InlineData(WaveTypeCode.TYPE_U2, WaveTypeCode.TYPE_U8)]
        [InlineData(WaveTypeCode.TYPE_U2, WaveTypeCode.TYPE_I8)]
        [InlineData(WaveTypeCode.TYPE_U2, WaveTypeCode.TYPE_I4)]
        public void CompatibleFalse(WaveTypeCode variable, WaveTypeCode value) 
            => Assert.False(variable.IsCompatibleNumber(value));

        [Theory]
        [InlineData(WaveTypeCode.TYPE_I4, WaveTypeCode.TYPE_U1)]
        [InlineData(WaveTypeCode.TYPE_I4, WaveTypeCode.TYPE_I1)]
        public void CompatibleTrue(WaveTypeCode variable, WaveTypeCode value) 
            => Assert.True(variable.IsCompatibleNumber(value));

        [Theory]
        [InlineData(typeof(SByteLiteralExpressionSyntax), WaveTypeCode.TYPE_I1)]
        [InlineData(typeof(Int16LiteralExpressionSyntax), WaveTypeCode.TYPE_I2)]
        [InlineData(typeof(Int32LiteralExpressionSyntax), WaveTypeCode.TYPE_I4)]
        [InlineData(typeof(Int64LiteralExpressionSyntax), WaveTypeCode.TYPE_I8)]
        [InlineData(typeof(ByteLiteralExpressionSyntax), WaveTypeCode.TYPE_U1)]
        [InlineData(typeof(UInt16LiteralExpressionSyntax), WaveTypeCode.TYPE_U2)]
        [InlineData(typeof(UInt32LiteralExpressionSyntax), WaveTypeCode.TYPE_U4)]
        [InlineData(typeof(UInt64LiteralExpressionSyntax), WaveTypeCode.TYPE_U8)]
        [InlineData(typeof(SingleLiteralExpressionSyntax), WaveTypeCode.TYPE_R4)]
        [InlineData(typeof(DoubleLiteralExpressionSyntax), WaveTypeCode.TYPE_R8)]
        [InlineData(typeof(DecimalLiteralExpressionSyntax), WaveTypeCode.TYPE_R16)]
        public void TypeCodeValidTest(Type t, WaveTypeCode code) 
            => Assert.Equal(code, CreateExpressionByType(t).GetTypeCode());

        [Theory]
        [InlineData(WaveTypeCode.TYPE_U1, (byte)22)]
        [InlineData(WaveTypeCode.TYPE_I1, (sbyte)-22)]
        [InlineData(WaveTypeCode.TYPE_I2, short.MaxValue / 2)]
        [InlineData(WaveTypeCode.TYPE_I4, int.MaxValue / 2)]
        [InlineData(WaveTypeCode.TYPE_I8, long.MaxValue / 2)]
        [InlineData(WaveTypeCode.TYPE_I8, long.MinValue / 2)]
        [InlineData(WaveTypeCode.TYPE_I4, int.MinValue / 2)]
        [InlineData(WaveTypeCode.TYPE_I2, short.MinValue / 2)]
        [InlineData(WaveTypeCode.TYPE_U2, ushort.MaxValue)]
        [InlineData(WaveTypeCode.TYPE_U4, uint.MaxValue)]
        [InlineData(WaveTypeCode.TYPE_U8, ulong.MaxValue)]
        public void DetectTypeCode(WaveTypeCode code, object value)
        {
            var str = value.ToString();
            var result =
                FieldDeclaratorSyntax.RedefineIntegerExpression(new UndefinedIntegerNumericLiteral(str)
                    .SetPos(new Position(0,0,0), 0), false);

            Assert.Equal(code, result.GetTypeCode());
        }

        private NumericLiteralExpressionSyntax CreateExpressionByType(Type t) =>
            (NumericLiteralExpressionSyntax) FormatterServices
                .GetUninitializedObject(t);
    }
}