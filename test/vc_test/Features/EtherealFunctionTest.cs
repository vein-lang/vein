namespace veinc_test.Features;

public class EtherealFunctionTest : TestContext
{
    [Test]
    public void AsTest()
        => Syntax.ethereal_function_expression("as").Positioned().ParseVein($"as<Type>(beta)");
    [Test]
    public void NameOfTest()
        => Syntax.ethereal_function_expression("nameof").Positioned().ParseVein($"nameof(beta)");
    [Test]
    public void TypeOfTest()
        => Syntax.ethereal_function_expression("typeof").Positioned().ParseVein($"typeof(beta)");

    [Test]
    public void IsTest()
        => Syntax.ethereal_function_expression("is").Positioned().ParseVein($"is<bool>(beta)");

    [Test]
    public void SizeOfTest()
        => Syntax.ethereal_function_expression("sizeof").Positioned().ParseVein($"sizeof<bool>()");

    [TestCase("as", "as<Type>(1 + 1)")]
    [TestCase("is", "is<Type>(1 + 1)")]
    [TestCase("nameof", "nameof<Type>(1 + 1)")]
    [TestCase("typeof", "typeof<Type>(1 + 1)")]

    [TestCase("as", "as<Type>(beta)")]
    [TestCase("is", "is<Type>(beta)")]
    [TestCase("nameof", "nameof<Type>(beta)")]
    [TestCase("typeof", "typeof<Type>(beta)")]

    [TestCase("as", "as<Type>(as<Type>(beta))")]
    [TestCase("is", "is<Type>(is<Type>(beta))")]
    [TestCase("nameof", "nameof<Type>(nameof<Type>(beta))")]
    [TestCase("typeof", "typeof<Type>(typeof<Type>(beta))")]

    [TestCase("sizeof", "sizeof<Type>()")]
    public void All(string keyword, string parseText)
    {
        var a1 = Syntax.ethereal_function_expression(keyword).Positioned().ParseVein(parseText);
        var a2 = Syntax.QualifiedExpression.Positioned().ParseVein($"1 + ({parseText})");
        var a3 = Syntax.QualifiedExpression.Positioned().ParseVein($"f1 == ({parseText})");
        var a4 = Syntax.ReturnStatement.Positioned().ParseVein($"return f1 == ({parseText});");

        List<IPassiveParseTransition> q = [a1, a2, a3, a4];

        Assert.True(q.All(x => !x.IsBrokenToken));
        Assert.True(q.All(x => x.Error is null));
    }
}
