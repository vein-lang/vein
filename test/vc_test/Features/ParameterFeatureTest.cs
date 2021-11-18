namespace veinc_test.Features;

public class ParameterFeatureTest : TestContext
{
    [Theory]
    [TestCase(" a: int", "a", "int", 0, null)]
    [TestCase("  b : SomeClass", "b", "SomeClass", 0, null)]
    [TestCase("lst: List<T>", "lst", "List", 0, null)]
    [TestCase("override b: int", "b", "int", 1, "override")]
    [TestCase("static c: int", "c", "int", 1, "static")]
    public void ParameterDeclarationParseTest(string parseStr, string name, string type, int lenMod, string mod)
    {
        var result = Syntax.ParameterDeclaration.ParseVein(parseStr);

        Assert.AreEqual(name, result.Identifier.ToString());
        Assert.AreEqual(type, result.Type.Identifier.ToString());
        Assert.AreEqual(lenMod, result.Modifiers.Count);
        if (mod is not null)
            Assert.AreEqual(mod, result.Modifiers[0].ModificatorKind.ToString().ToLower());
    }

    [Theory]
    [TestCase(" a int")]
    [TestCase("bla!")]
    [TestCase("b@b")]
    [TestCase("b@b: int")]
    [TestCase("b@b: int : int")]
    [TestCase("43534")]
    [TestCase("):s")]
    public void ParameterDeclarationParseTestFail(string parseStr) =>
        Assert.Throws<VeinParseException>(() => Syntax.ParameterDeclaration.ParseVein(parseStr));

    [Theory]
    [TestCase("operation test[x: int32] -> int32", "test", false)]
    [TestCase("operation test[] -> foo22", "test", false)]
    [TestCase("operation asd_d2[] -> foo22", "asd_d2", false)]
    [TestCase("operation asd_d2[i: s, x: w] -> foo22", "asd_d2", false)]
    [TestCase("operation asd-d[i: s, x: w] -> foo22", "asd-d2", true)]
    [TestCase("operation 123[i: s, x: w] -> foo22", "123", true)]
    [TestCase("operation $[i: s, x: w] -> foo22", "$", true)]
    [TestCase("operation name[ s s s s] -> foo22", "name", true)]
    [TestCase("operation name[i: s, x: w] - foo22", "name", true)]
    [TestCase("operation name[i: s, x: w]", "name", true)]
    public void MethodParametersTest(string parseStr, string name, bool needFail)
    {
        var a = new VeinSyntax();
        var d = default(MethodDeclarationSyntax);

        if (needFail)
        {
            Assert.Throws<VeinParseException>(() => d = a.OperationDeclaration
                .ParseVein(parseStr + "{body{}}"));
        }
        else
        {
            d = a.OperationDeclaration
                .ParseVein(parseStr + "{body{}}");
            Assert.AreEqual(name, d.Identifier.ToString());
        }
    }

    [Test]
    public void FullsetMethodParametersAndBodyTest()
    {
        var a = new VeinSyntax();
        var d = a.ClassDeclaration
            .ParseVein("public class DDD { public test(x: int32): void { } }");

        Assert.False(d.IsStruct);
        Assert.False(d.IsInterface);
        Assert.AreEqual("DDD", d.Identifier.ToString());
        IshtarAssert.Contains(d.Modifiers, x => x.ModificatorKind == ModificatorKind.Public);
        var method = d.Methods.Single();
        Assert.AreEqual("test", method.Identifier.ToString());
        IshtarAssert.Contains(method.Modifiers, x => x.ModificatorKind == ModificatorKind.Public);
        Assert.AreEqual("void", method.ReturnType.Identifier.ToString().ToLower());

        var @params = method.Parameters.Single();
        Assert.AreEqual("x", @params.Identifier.ToString());
        Assert.AreEqual("int32", @params.Type.Identifier.ToString().ToLower());
    }
}
