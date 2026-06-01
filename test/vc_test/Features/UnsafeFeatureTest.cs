namespace veinc_test.Features;

using vein.syntax;

public class UnsafeFeatureTest
{
    public static VeinSyntax Syntax => new();

    [Test]
    public void ParseUnsafeBlock()
    {
        var result = Syntax.UnsafeStatement.ParseVein("unsafe { }");
        Assert.IsInstanceOf<UnsafeBlockSyntax>(result);
        Assert.IsNotNull(result.Body);
    }

    [Test]
    public void ParseUnsafeBlockWithStatements()
    {
        var result = Syntax.UnsafeStatement.ParseVein(
            """
            unsafe {
                auto x = 1;
            }
            """);
        Assert.IsInstanceOf<UnsafeBlockSyntax>(result);
        Assert.AreEqual(1, result.Body.Statements.Count);
    }

    [Test]
    public void ParseUnsafeModifierOnMethod()
    {
        var cd = Syntax.ClassDeclaration.ParseVein(
            """
            public class Foo {
                public unsafe bar(): void {}
            }
            """);
        Assert.AreEqual(1, cd.Methods.Count);
        var method = cd.Methods[0];
        Assert.IsTrue(method.Modifiers.Any(m => m.ModificatorKind == ModificatorKind.Unsafe));
    }

    [Test]
    public void ParseStackAllocExpression()
    {
        var result = Syntax.QualifiedExpression.ParseVein("stackalloc i32[10]");
        Assert.IsInstanceOf<StackAllocExpressionSyntax>(result);
        var sa = (StackAllocExpressionSyntax)result;
        Assert.AreEqual("i32", sa.ElementType.Identifier.ExpressionString);
    }

    [Test]
    public void ParseStackAllocExpressionWithVariable()
    {
        var result = Syntax.QualifiedExpression.ParseVein("stackalloc i32[n]");
        Assert.IsInstanceOf<StackAllocExpressionSyntax>(result);
    }

    [Test]
    public void ParseAddressOf()
    {
        var result = Syntax.QualifiedExpression.ParseVein("&x");
        Assert.IsInstanceOf<UnaryExpressionSyntax>(result);
        var unary = (UnaryExpressionSyntax)result;
        Assert.AreEqual(System.Linq.Expressions.ExpressionType.And, unary.OperatorType);
        Assert.IsInstanceOf<IdentifierExpression>(unary.Operand);
    }

    [Test]
    public void ParsePointerDereference()
    {
        var result = Syntax.QualifiedExpression.ParseVein("*ptr");
        Assert.IsInstanceOf<UnaryExpressionSyntax>(result);
        var unary = (UnaryExpressionSyntax)result;
        Assert.AreEqual(System.Linq.Expressions.ExpressionType.Multiply, unary.OperatorType);
    }

    [Test]
    public void ParsePointerType()
    {
        var cd = Syntax.ClassDeclaration.ParseVein("public class Foo { public p: i32*; }");
        Assert.AreEqual(1, cd.Fields.Count);
        var field = cd.Fields[0];
        Assert.IsTrue(field.Type.IsPointer);
    }

    [Test]
    public void ParsePointerArithmetic()
    {
        var result = Syntax.QualifiedExpression.ParseVein("ptr + 1");
        Assert.IsInstanceOf<BinaryExpressionSyntax>(result);
        var bin = (BinaryExpressionSyntax)result;
        Assert.AreEqual(System.Linq.Expressions.ExpressionType.Add, bin.OperatorType);
    }

    [Test]
    public void UnsafeKeywordIsRecognized()
    {
        Assert.IsTrue(vein.stl.VeinKeywords.list.Contains("unsafe"));
        Assert.IsTrue(vein.stl.VeinKeywords.list.Contains("stackalloc"));
    }

    [Test]
    public void UnsafeModificatorKindExists()
    {
        Assert.DoesNotThrow(() => _ = ModificatorKind.Unsafe);
    }
}
