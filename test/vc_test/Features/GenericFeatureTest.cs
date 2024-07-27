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

}
