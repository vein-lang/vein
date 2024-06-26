namespace veinc_test.Features;

public class AliasFeatureTest : TestContext
{
    [Test]
    public void ClassicAlias()
    {
        var a1 = Syntax.AliasDeclaration.Positioned().ParseVein($"alias foo <| TestType;");

        Assert.IsTrue(a1.IsType);

        var a2 = Syntax.AliasDeclaration.Positioned().ParseVein($"global alias foo <| TestType;");

        Assert.IsTrue(a2.IsType);
        Assert.IsTrue(a2.IsGlobal);
    }

    [Test]
    public void AliasForMethodDeclaration()
    {
        var beforeMethodTest = Syntax.MethodParametersAndBody.Token().ParseVein("(i: i32, f: String): void;");


        var a1 = Syntax.AliasDeclaration.Positioned().ParseVein($"alias foo <| (i: i32, f: String): void;");

        Assert.IsFalse(a1.IsType);
        Assert.IsTrue(a1.IsMethod);


        var a2 = Syntax.AliasDeclaration.Positioned().ParseVein($"global alias foo <| (i: i32, f: String): void;");

        Assert.IsFalse(a2.IsType);
        Assert.IsTrue(a2.IsMethod);
        Assert.IsTrue(a2.IsGlobal);
    }
}
