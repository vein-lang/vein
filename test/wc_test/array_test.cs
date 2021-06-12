namespace wc_test
{
    using System.Linq;
    using mana.stl;
    using mana.syntax;
    using Xunit;
    using Xunit.Abstractions;

    public class array_test
    {
        private readonly ITestOutputHelper _debug;
        public static ManaSyntax Syntax => new();

        public array_test(ITestOutputHelper testOutputHelper) => _debug = testOutputHelper;


        [Fact]
        public void FirstArrayTest()
            => Syntax.ClassMemberDeclaration.ParseMana($"public x: arr[];");

        [Fact(Skip = "Disabled")]
        public void AccessArrayTest()
        {
            var result = Syntax
                .Statement
                .ParseMana($"{{x.x.x[1, 2, 3, \"\", variable, 8 * 8]}}");

            Assert.False(result.IsBrokenToken);
        }

        [Fact]
        public void ArrayCompilationTest()
        {
            var result = Syntax.new_expression.ParseMana("new Foo[5]")
                .As<NewExpressionSyntax>();

            Assert.True(result.IsArray);
            var arr = result.CtorArgs.As<ArrayInitializerExpression>().Sizes.ToArray();
            Assert.Single(arr);
            var i4 = Assert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
            Assert.Equal(5, i4.Value);
        }

        [Fact]
        public void ArrayCompilationTest2()
        {
            var result = Syntax.new_expression.ParseMana("new Foo[5] { 1, 2, 3, 4, 5 }")
                .As<NewExpressionSyntax>();

            Assert.True(result.IsArray);
            var ctor = result.CtorArgs.As<ArrayInitializerExpression>();

            var arr = ctor.Sizes.ToArray();
            Assert.Single(arr);
            var i4 = Assert.IsType<Int32LiteralExpressionSyntax>(arr[0]);
            Assert.Equal(5, i4.Value);

            Assert.NotNull(ctor.Args);
            Assert.Equal(5, ctor.Args.FillArgs.Length);
            Assert.True(ctor.Args.FillArgs
                .Select(x => x.As<Int32LiteralExpressionSyntax>())
                .Select(x => x.Value)
                .ToArray()
                .SequenceEqual(new [] {1,2,3,4,5}));
        }
    }
}
