namespace veinc_test.Features;

public class FaultFeatureTest
{
    public static VeinSyntax Syntax => new();

    [Test]
    public void Test1()
    {
        var output = Syntax.FailStatement.Positioned().ParseVein(
            """
            fail new Fault();
            """);
        Assert.NotNull(output.Transform);
        Assert.AreEqual(SyntaxType.FailStatement, output.Kind);
        Assert.AreEqual(SyntaxType.NewExpression, output.Expression.Kind);
    }

    [Test]
    public void Test2()
    {
        var output = Syntax.FailStatement.Positioned().ParseVein(
            """
            fail null;
            """);
        Assert.NotNull(output.Transform);
        Assert.AreEqual(SyntaxType.FailStatement, output.Kind);
        Assert.AreEqual(SyntaxType.LiteralExpression, output.Expression.Kind);
    }

    [Test]
    public void Test3()
    {
        var output = Syntax.FailStatement.Positioned().ParseVein(
            """
            fail new Fault("test");
            """);
        Assert.NotNull(output.Transform);
        Assert.AreEqual(SyntaxType.FailStatement, output.Kind);
        Assert.AreEqual(SyntaxType.NewExpression, output.Expression.Kind);
        if (output.Expression is NewExpressionSyntax @new)
        {
            Assert.AreEqual("Fault", @new.TargetType.ExpressionString);
            Assert.IsTrue(@new.IsObject);
        }
    }

    [Test]
    public void FullTest()
    {
        var output = Syntax.CompilationUnit.ParseVein(
            """
            #space "test"
            #use "vein/lang"
            
            struct App {
                public test1(): void {
                    fail new Fault();
                }
            
                public test2(): void {
                    fail null;
                }
                
                public test3(): void {
                    fail new Fault("gsdffg");
                }
                
                public test4(): void {
                    fail new Fault("gsdffg", 5, 78);
                }
            }
            """);

        var clazz = output.Members.Single();

        if (clazz is not ClassDeclarationSyntax clzDecl)
        {
            Assert.Fail();
            return;
        }

        var methods = clzDecl.Methods;

        Assert.AreEqual(4, methods.Count);
    }
}
