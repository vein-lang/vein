namespace wc_test
{
    using System;
    using System.Linq;
    using vein.stl;
    using vein.syntax;
    using NUnit.Framework;

    public class array_test
    {
        public static ManaSyntax Syntax => new();

        [Test]
        public void FirstArrayTest()
            => Syntax.ClassMemberDeclaration.ParseMana($"public x: arr[];");

        [Test]
        public void AccessArrayTest()
        {
            Assert.Ignore("TODO");
            var result = Syntax
                .Statement
                .ParseMana($"{{x.x.x[1, 2, 3, \"\", variable, 8 * 8]}}");

            Assert.False(result.IsBrokenToken);
        }

        [Test]
        public void ArrayCompilationTest()
        {
            var result = Syntax.new_expression.ParseMana("new Foo[5]")
                .As<NewExpressionSyntax>();
            Assert.True(result.IsArray);
            var arr = result.CtorArgs.As<ArrayInitializerExpression>().Sizes.ToArray();
            IshtarAssert.Single(arr);
            var i4 = IshtarAssert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
            Assert.AreEqual(5, i4.Value);
        }

        [Test]
        public void ArrayCompilationTest2()
        {
            var result = Syntax.new_expression.ParseMana("new Foo[5] { 1, 2, 3, 4, 5 }")
                .As<NewExpressionSyntax>();

            Assert.True(result.IsArray);
            var ctor = result.CtorArgs.As<ArrayInitializerExpression>();

            var arr = ctor.Sizes.ToArray();
            IshtarAssert.Single(arr);
            var i4 = IshtarAssert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
            Assert.AreEqual(5, i4.Value);

            Assert.NotNull(ctor.Args);
            Assert.AreEqual(5, ctor.Args.FillArgs.Length);
            Assert.True(ctor.Args.FillArgs
                .Select(x => x.As<Int32LiteralExpressionSyntax>())
                .Select(x => x.Value)
                .ToArray()
                .SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
        }
    }
}
