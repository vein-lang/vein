namespace veinc_test.Features;

public class DirectivesFeatureTest : TestContext
{
    [Test]
    public void FooUseTest() => Syntax.UseSyntax.End().ParseVein("#use \"boo\"");

    [Test]
    public void SpaceDirectiveTest()
    {
        var a = new VeinSyntax();
        var d = a.SpaceSyntax
            .ParseVein("#space \"foo\"");
        Assert.AreEqual("foo", d.Value.Token);
    }

    [Test]
    public void UseDirectiveTest()
    {
        var d = Syntax.UseSyntax
            .ParseVein("#use \"stl.lib\"") as UseSyntax;
        Assert.AreEqual("stl.lib", d.Value.Token);
    }

    [Test]
    public void FooProgramTest() =>
        Syntax.CompilationUnit.End().ParseVein(
            "#use \"stl.lib\"\n" +
            "public class Foo {" +
            "public master(): void {}" +
            "}");

    [Test]
    public void CtorAndDtorTest()
    {
        Syntax.CompilationUnit.End().ParseVein(
            "#use \"stl.lib\"\n" +
            "public class Foo {" +
            "public new() {}" +
            "}");
        Syntax.CompilationUnit.End().ParseVein(
            "#use \"stl.lib\"\n" +
            "public class Foo {" +
            "public delete() {}" +
            "}");
        Syntax.CompilationUnit.End().ParseVein(
            "#use \"stl.lib\"\n" +
            "public class Foo {" +
            "public new(s:S) {}" +
            "}");
    }
}
