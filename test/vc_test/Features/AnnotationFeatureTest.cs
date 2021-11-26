namespace veinc_test.Features;

public class AnnotationFeatureTest
{
    public static VeinSyntax Syntax => new();


    [Theory]
    [TestCase("native(\"foo\")")]
    public void AnnotationWithArgsTest(string str)
    {
        var result = Syntax.AspectSyntax.End().ParseVein(str);
        Assert.IsNotEmpty(result.Args);
    }

    [Test]
    public void AnnotationTest()
    {
        var a = new VeinSyntax();
        var d = a.AspectsExpression.End().ParseVein("[special, native]");
        Assert.AreEqual(2, d.Length);
    }
}
