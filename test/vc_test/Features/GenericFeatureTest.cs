namespace veinc_test.Features;

public class GenericFeatureTest : TestContext
{
    [Theory]
    [TestCase("class", "<T>")]
    [TestCase("struct", "<T>")]
    [TestCase("interface", "<T>")]
    [TestCase("class", "<T1, T2, T3, T4, T5, T6, T7>", "when T1 is i32")]
    public void DeclarationCanDeclareMethods(string keyword, string generics, string extended = "")
    {
        var cd = Syntax.ClassDeclaration.Parse($"[special] {keyword} Program{generics} {extended} {{ [special] main(): void {{}} }}");
        Assert.True(cd.Methods.Any());
        Assert.True(cd.Identifier.ToString().StartsWith("Program<T"));
        Assert.IsTrue(cd.Aspects.Single().IsSpecial);

        var md = cd.Methods.Single();
        Assert.AreEqual("void", md.ReturnType.Identifier.ToString().ToLower());
        Assert.AreEqual("main", md.Identifier.ToString());
        Assert.IsTrue(md.Aspects.Single().IsSpecial);
        Assert.False(md.Parameters.Any());

        Assert.Throws<VeinParseException>(() => Syntax.ClassDeclaration.ParseVein($" class Test{generics} {{ void Main }}"));
        Assert.Throws<VeinParseException>(() => Syntax.ClassDeclaration.ParseVein($"class Foo{generics} {{ int main() }}"));
    }


    [Test]
    public void GenericConstraintParserTest()
    {
        var cd = Syntax.GenericConstraintParser.ParseVein("when T is i32");
        Assert.NotZero(cd.Count);
        var c = cd.First();

        Assert.AreEqual(c.GenericIndex.Typeword.Identifier.ExpressionString, "T");
        Assert.AreEqual(c.Constraint.Typeword.Identifier.ExpressionString, "i32");
    }

    [Test]
    public void ManyGenericConstraintParserTest()
    {
        var cd = Syntax.GenericConstraintParser.ParseVein("when T is i32, T1 is bool");
        Assert.NotZero(cd.Count);
        var c = cd.First();

        Assert.AreEqual(c.GenericIndex.Typeword.Identifier.ExpressionString, "T");
        Assert.AreEqual(c.Constraint.Typeword.Identifier.ExpressionString, "i32");
    }


    [Test]
    public void GenericsDeclarationParser()
    {
        var cd = Syntax.GenericsDeclarationParser.ParseVein("<T>");
        Assert.NotZero(cd.Count);
        var c = cd.First();
    }

    [Test]
    public void ManyGenericsDeclarationParser()
    {
        var cd = Syntax.GenericsDeclarationParser.ParseVein("<T, T1, T2, T3>");
        Assert.NotZero(cd.Count);
        var c = cd.First();
    }

    [Test]
    public void GenericMethod_DeclarationParses()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public class C { id<T>(x: T): T { return x; } }");
        var m = cd.Methods.Single();
        Assert.AreEqual("id", m.Identifier.ToString());
        Assert.AreEqual(1, m.GenericTypes.Count);
        Assert.AreEqual("T", m.GenericTypes[0].Typeword.Identifier.ToString());
        Assert.AreEqual("T", m.ReturnType.Identifier.ToString());
    }

    [Test]
    public void CallSite_TypeArguments_Parse()
    {
        var inv = Syntax.QualifiedExpression.End().ParseVein("id<i32>(42)") as InvocationExpression;
        Assert.IsNotNull(inv, "expected a bare InvocationExpression for an unqualified generic call");
        Assert.AreEqual(1, inv.TypeArguments.Count);
        Assert.AreEqual("i32", inv.TypeArguments[0].Typeword.Identifier.ToString());
        Assert.AreEqual(1, inv.Arguments.Arguments.Count());
    }

    [Test]
    public void CallSite_NoTypeArguments_IsEmpty()
    {
        var inv = Syntax.QualifiedExpression.End().ParseVein("foo(42)") as InvocationExpression;
        Assert.IsNotNull(inv);
        Assert.AreEqual(0, inv.TypeArguments.Count);
    }

    [Test]
    public void MultiTypeArguments_Parse()
    {
        var inv = Syntax.QualifiedExpression.End().ParseVein("pair<i32, string>(1, x)") as InvocationExpression;
        Assert.IsNotNull(inv);
        Assert.AreEqual(2, inv.TypeArguments.Count);
        Assert.AreEqual("i32", inv.TypeArguments[0].Typeword.Identifier.ToString());
        Assert.AreEqual("String", inv.TypeArguments[1].Typeword.Identifier.ToString()); // `string` normalizes to String
    }

}
