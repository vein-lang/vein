namespace veinc_test.Features;

public class MethodFeatureTest : TestContext
{
    [Theory]
    [TestCase("class")]
    [TestCase("struct")]
    [TestCase("interface")]
    public void DeclarationCanDeclareMethods(string keyword)
    {
        var cd = Syntax.ClassDeclaration.Parse($"[special] {keyword} Program {{ [special] main(): void {{}} }}");
        Assert.True(cd.Methods.Any());
        Assert.AreEqual("Program", cd.Identifier.ToString());
        Assert.IsTrue(cd.Aspects.Single().IsSpecial);

        var md = cd.Methods.Single();
        Assert.AreEqual("void", md.ReturnType.Identifier.ToString().ToLower());
        Assert.AreEqual("main", md.Identifier.ToString());
        Assert.IsTrue(md.Aspects.Single().IsSpecial);
        Assert.False(md.Parameters.Any());

        Assert.Throws<VeinParseException>(() => Syntax.ClassDeclaration.ParseVein(" class Test { void Main }"));
        Assert.Throws<VeinParseException>(() => Syntax.ClassDeclaration.ParseVein("class Foo { int main() }"));
    }

    [Test]
    public void SelfReturnTypeTest()
    {
        var method = Syntax.MethodDeclaration.End().ParseVein("foo(): self { return this; }") as MethodDeclarationSyntax;

        Assert.IsTrue(method.ReturnType.IsSelf);
    }
}
