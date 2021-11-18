namespace veinc_test.Features;

public class BlockFeatureTest : TestContext
{
    [Test]
    public void VariableStatementTest()
    {
        var result = Syntax.Block.End().ParseVein(@"{
    auto f = 12;

    return 1;
    return 2;
}");
        Assert.False(result.IsBrokenToken);
        foreach (var statement in result.Statements)
            Assert.False(statement.IsBrokenToken);
    }
}
