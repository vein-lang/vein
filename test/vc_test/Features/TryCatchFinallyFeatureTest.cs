namespace veinc_test.Features;

public class TryCatchFinallyFeatureTest
{
    public static VeinSyntax Syntax => new();


    [Test]
    public void BaseParseTest1()
        => Syntax.TryStatement.End().ParseVein($"try {{}} catch {{}} finally {{}} ");
    [Test]
    public void BaseParseTest2()
        => Syntax.TryStatement.End().ParseVein($"try {{}} catch {{}}");
    [Test]
    public void BaseParseTest3()
        => Syntax.TryStatement.End().ParseVein($"try {{}} finally {{}}");
    [Test]
    public void BaseParseTest4()
        => Syntax.TryStatement.End().ParseVein($"try {{}} catch {{}} catch {{}} finally {{}}");
    [Test]
    public void BaseParseTest5()
        => Syntax.TryStatement.End().ParseVein($"try {{}} catch(:any) {{}}");
    [Test]
    public void BaseParseTest6()
        => Syntax.TryStatement.End().ParseVein($"try {{}} catch(x:any) {{}}");

    [Test]
    public void FailParseTest() => Assert.Throws<VeinParseException>(() =>
        Syntax.TryStatement.End().ParseVein($"try {{}} finally {{}} catch {{}}"));

    [Test]
    public void TryCatchWithType()
    {
        var r = Syntax.TryStatement.End().ParseVein($"try {{}} catch(x:any) {{}}");
        Assert.NotNull(r.TryBlock);
        Assert.NotNull(r.Catches);
        Assert.IsNotEmpty(r.Catches);
        Assert.Null(r.Finally);
        Assert.AreEqual(r.Catches.Count(), 1);
        var @catch = r.Catches.Single();

        Assert.NotNull(@catch.Block);
        Assert.NotNull(@catch.Specifier);
        Assert.AreEqual($"{@catch.Specifier.Type.Typeword.Identifier}", "any");
        Assert.AreEqual($"{@catch.Specifier.Identifier.GetOrDefault()}", "x");
    }
}
