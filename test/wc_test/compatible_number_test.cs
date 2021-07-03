namespace wc_test
{
    using System;
    using System.Runtime.Serialization;
    using Sprache;
    using ishtar;
    using mana.runtime;
    using mana.syntax;
    using NUnit.Framework;

    [TestFixture]
    public class compatible_number_test
    {
        [Test]
        [TestCase(ManaTypeCode.TYPE_R4, ManaTypeCode.TYPE_R8)]
        [TestCase(ManaTypeCode.TYPE_R4, ManaTypeCode.TYPE_R16)]
        [TestCase(ManaTypeCode.TYPE_R8, ManaTypeCode.TYPE_R16)]
        [TestCase(ManaTypeCode.TYPE_R16, ManaTypeCode.TYPE_R4)]
        [TestCase(ManaTypeCode.TYPE_R16, ManaTypeCode.TYPE_R8)]
        [TestCase(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_U4)]
        [TestCase(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_U8)]
        [TestCase(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_I8)]
        [TestCase(ManaTypeCode.TYPE_U2, ManaTypeCode.TYPE_I4)]
        public void CompatibleFalse(ManaTypeCode variable, ManaTypeCode value)
            => Assert.False(variable.IsCompatibleNumber(value));

        [Test]
        [TestCase(ManaTypeCode.TYPE_I4, ManaTypeCode.TYPE_U1)]
        [TestCase(ManaTypeCode.TYPE_I4, ManaTypeCode.TYPE_I1)]
        public void CompatibleTrue(ManaTypeCode variable, ManaTypeCode value)
            => Assert.True(variable.IsCompatibleNumber(value));

        [Test]
        [TestCase(typeof(SByteLiteralExpressionSyntax), ManaTypeCode.TYPE_I1)]
        [TestCase(typeof(Int16LiteralExpressionSyntax), ManaTypeCode.TYPE_I2)]
        [TestCase(typeof(Int32LiteralExpressionSyntax), ManaTypeCode.TYPE_I4)]
        [TestCase(typeof(Int64LiteralExpressionSyntax), ManaTypeCode.TYPE_I8)]
        [TestCase(typeof(ByteLiteralExpressionSyntax), ManaTypeCode.TYPE_U1)]
        [TestCase(typeof(UInt16LiteralExpressionSyntax), ManaTypeCode.TYPE_U2)]
        [TestCase(typeof(UInt32LiteralExpressionSyntax), ManaTypeCode.TYPE_U4)]
        [TestCase(typeof(UInt64LiteralExpressionSyntax), ManaTypeCode.TYPE_U8)]
        [TestCase(typeof(SingleLiteralExpressionSyntax), ManaTypeCode.TYPE_R4)]
        [TestCase(typeof(DoubleLiteralExpressionSyntax), ManaTypeCode.TYPE_R8)]
        [TestCase(typeof(DecimalLiteralExpressionSyntax), ManaTypeCode.TYPE_R16)]
        public void TypeCodeValidTest(Type t, ManaTypeCode code)
            => Assert.AreEqual(code, CreateExpressionByType(t).GetTypeCode());

        [Test]
        [TestCase(ManaTypeCode.TYPE_U1, (byte)22)]
        [TestCase(ManaTypeCode.TYPE_I1, (sbyte)-22)]
        [TestCase(ManaTypeCode.TYPE_I2, short.MaxValue / 2)]
        [TestCase(ManaTypeCode.TYPE_I4, int.MaxValue / 2)]
        [TestCase(ManaTypeCode.TYPE_I8, long.MaxValue / 2)]
        [TestCase(ManaTypeCode.TYPE_I8, long.MinValue / 2)]
        [TestCase(ManaTypeCode.TYPE_I4, int.MinValue / 2)]
        [TestCase(ManaTypeCode.TYPE_I2, short.MinValue / 2)]
        [TestCase(ManaTypeCode.TYPE_U2, ushort.MaxValue)]
        [TestCase(ManaTypeCode.TYPE_U4, uint.MaxValue)]
        [TestCase(ManaTypeCode.TYPE_U8, ulong.MaxValue)]
        public void DetectTypeCode(ManaTypeCode code, object value)
        {
            var str = value.ToString();
            var result =
                FieldDeclaratorSyntax.RedefineIntegerExpression(new UndefinedIntegerNumericLiteral(str)
                    .SetPos(new Position(0,0,0), 0), false);

            Assert.AreEqual(code, result.GetTypeCode());
        }

        private NumericLiteralExpressionSyntax CreateExpressionByType(Type t) =>
            (NumericLiteralExpressionSyntax)FormatterServices
                .GetUninitializedObject(t);
    }
}
