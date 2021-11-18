namespace veinc_test;

[TestFixture]
public class CompatibleNumberTest
{
    [Test]
    [TestCase(VeinTypeCode.TYPE_R4, VeinTypeCode.TYPE_R8)]
    [TestCase(VeinTypeCode.TYPE_R4, VeinTypeCode.TYPE_R16)]
    [TestCase(VeinTypeCode.TYPE_R8, VeinTypeCode.TYPE_R16)]
    [TestCase(VeinTypeCode.TYPE_R16, VeinTypeCode.TYPE_R4)]
    [TestCase(VeinTypeCode.TYPE_R16, VeinTypeCode.TYPE_R8)]
    [TestCase(VeinTypeCode.TYPE_U2, VeinTypeCode.TYPE_U4)]
    [TestCase(VeinTypeCode.TYPE_U2, VeinTypeCode.TYPE_U8)]
    [TestCase(VeinTypeCode.TYPE_U2, VeinTypeCode.TYPE_I8)]
    [TestCase(VeinTypeCode.TYPE_U2, VeinTypeCode.TYPE_I4)]
    public void CompatibleFalse(VeinTypeCode variable, VeinTypeCode value)
        => Assert.False(variable.IsCompatibleNumber(value));

    [Test]
    [TestCase(VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_U1)]
    [TestCase(VeinTypeCode.TYPE_I4, VeinTypeCode.TYPE_I1)]
    public void CompatibleTrue(VeinTypeCode variable, VeinTypeCode value)
        => Assert.True(variable.IsCompatibleNumber(value));

    [Test]
    [TestCase(typeof(SByteLiteralExpressionSyntax), VeinTypeCode.TYPE_I1)]
    [TestCase(typeof(Int16LiteralExpressionSyntax), VeinTypeCode.TYPE_I2)]
    [TestCase(typeof(Int32LiteralExpressionSyntax), VeinTypeCode.TYPE_I4)]
    [TestCase(typeof(Int64LiteralExpressionSyntax), VeinTypeCode.TYPE_I8)]
    [TestCase(typeof(ByteLiteralExpressionSyntax), VeinTypeCode.TYPE_U1)]
    [TestCase(typeof(UInt16LiteralExpressionSyntax), VeinTypeCode.TYPE_U2)]
    [TestCase(typeof(UInt32LiteralExpressionSyntax), VeinTypeCode.TYPE_U4)]
    [TestCase(typeof(UInt64LiteralExpressionSyntax), VeinTypeCode.TYPE_U8)]
    [TestCase(typeof(SingleLiteralExpressionSyntax), VeinTypeCode.TYPE_R4)]
    [TestCase(typeof(DoubleLiteralExpressionSyntax), VeinTypeCode.TYPE_R8)]
    [TestCase(typeof(DecimalLiteralExpressionSyntax), VeinTypeCode.TYPE_R16)]
    public void TypeCodeValidTest(Type t, VeinTypeCode code)
        => Assert.AreEqual(code, CreateExpressionByType(t).GetTypeCode());

    [Test]
    [TestCase(VeinTypeCode.TYPE_U1, (byte)22)]
    [TestCase(VeinTypeCode.TYPE_I1, (sbyte)-22)]
    [TestCase(VeinTypeCode.TYPE_I2, short.MaxValue / 2)]
    [TestCase(VeinTypeCode.TYPE_I4, int.MaxValue / 2)]
    [TestCase(VeinTypeCode.TYPE_I8, long.MaxValue / 2)]
    [TestCase(VeinTypeCode.TYPE_I8, long.MinValue / 2)]
    [TestCase(VeinTypeCode.TYPE_I4, int.MinValue / 2)]
    [TestCase(VeinTypeCode.TYPE_I2, short.MinValue / 2)]
    [TestCase(VeinTypeCode.TYPE_U2, ushort.MaxValue)]
    [TestCase(VeinTypeCode.TYPE_U4, uint.MaxValue)]
    [TestCase(VeinTypeCode.TYPE_U8, ulong.MaxValue)]
    public void DetectTypeCode(VeinTypeCode code, object value)
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
