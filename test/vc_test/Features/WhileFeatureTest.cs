namespace veinc_test.Features;

public class WhileFeatureTest
{
    public static VeinSyntax Vein => new();


    [Test]
    public void Test1()
    {
        Vein.KeywordExpression("while").Positioned().Token().End().ParseVein("while");

        var output = Vein.WhileStatement.Positioned().ParseVein(
            """
            while(true)
            {
                i++;
                Out.print("hello world!");
            }
            """);
    }

    [Test]
    public void Test2()
    {
        var output = Vein.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"
            
            struct App {
                public test1(): void {
                    while(true)
                    {
                        i++;
                        Out.print("hello world!");
                    }
                }
            }
            """);
    }
}
