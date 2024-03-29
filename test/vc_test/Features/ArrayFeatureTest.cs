namespace veinc_test;

public class ArrayFeatureTest
{
    public static VeinSyntax Syntax => new();

    [Test]
    public void FirstArrayTest()
        => Syntax.ClassMemberDeclaration.ParseVein($"public x: arr[];");

    [Test]
    public void AccessArrayTest()
    {
        Assert.Ignore("TODO");
        var result = Syntax
            .Statement
            .ParseVein($"{{x.x.x[1, 2, 3, \"\", variable, 8 * 8]}}");

        Assert.False(result.IsBrokenToken);
    }

    [Test]
    public void ArrayCompilationTest()
    {
        var result = Syntax.new_expression.ParseVein("new Foo[5]")
            .As<NewExpressionSyntax>();
        Assert.True(result.IsArray);
        var arr = result.CtorArgs.As<ArrayInitializerExpression>().Sizes.ToArray();
        IshtarAssert.Single(arr);
        var i4 = IshtarAssert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
        Assert.AreEqual(5, i4.Value);
    }

    [Test]
    public void ArrayCompilationTest2()
    {
        var result = Syntax.new_expression.ParseVein("new Foo[5] { 1, 2, 3, 4, 5 }")
            .As<NewExpressionSyntax>();

        Assert.True(result.IsArray);
        var ctor = result.CtorArgs.As<ArrayInitializerExpression>();

        var arr = ctor.Sizes.ToArray();
        IshtarAssert.Single(arr);
        var i4 = IshtarAssert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
        Assert.AreEqual(5, i4.Value);

        Assert.NotNull(ctor.Args);
        Assert.AreEqual(5, ctor.Args.FillArgs.Length);
        Assert.True(ctor.Args.FillArgs
            .Select(x => x.As<Int32LiteralExpressionSyntax>())
            .Select(x => x.Value)
            .ToArray()
            .SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
    }
}
