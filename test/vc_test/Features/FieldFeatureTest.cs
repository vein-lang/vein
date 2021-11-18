namespace veinc_test.Features;

public class FieldFeatureTest : TestContext
{
    [Test]
    public void FieldTest()
        => Syntax.FieldDeclaration.ParseVein("public const MaxValue: Int16 = 32767;");

    [Test]
    public void FieldWithAnnotationTest()
        => Syntax.FieldDeclaration.ParseVein("[native] private _value: Int16;");

    [Test]
    public void LiteralAssignedExpressionTest()
    {
        var result = Syntax.FieldDeclaration.End().ParseVein("foo: Int32 = -22;");
        Assert.NotNull(result);
        Assert.AreEqual("int32", result.Type.Identifier.ToString().ToLower());
        Assert.AreEqual("foo", result.Field.Identifier.ToString());
        Assert.AreEqual("(22)", result.Field.Expression.ExpressionString);
        IshtarAssert.IsType<UnaryExpressionSyntax>(result.Field.Expression);
    }

    [Theory]
    [TestCase("foo: Type;")]
    [TestCase("[special] foo: Type;")]
    [TestCase("[special] public foo: Type;")]
    public void FieldTest00(string str)
        => Syntax.FieldDeclaration.ParseVein(str);
}
