namespace veinc_test.Features;

public class ForFeatureTest
{
    public static VeinSyntax Syntax => new();

    [Test]
    public void Test1() =>
        Syntax.for_statement.ParseVein(
            """
            for(let i = 0; i != 15; i++) {
                Out.print("hello world!");
            }
            """);
}
