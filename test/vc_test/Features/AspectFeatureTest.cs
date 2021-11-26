namespace veinc_test.Features;

public class AspectFeatureTest
{
    public static VeinSyntax Syntax => new();


    [Theory]
    [TestCase("native(\"foo\")")]
    public void AspectWithArgsTest(string str)
    {
        var result = Syntax.AspectSyntax.End().ParseVein(str);
        Assert.IsNotEmpty(result.Args);
    }

    [Test]
    public void AspectTest()
    {
        var a = new VeinSyntax();
        var d = a.AspectsExpression.End().ParseVein("[special, native]");
        Assert.AreEqual(2, d.Length);
    }

    [Test]
    public void aspectWithArgsTest()
    {
        var a = new VeinSyntax();
        var d = a.AspectsExpression.End().ParseVein("[special, readonly, forwarded, alias(\"bool\")]");
        Assert.AreEqual(4, d.Length);
    }
}
