namespace wc_test
{
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
            var result = Syntax.new_expression.ParseMana("new Foo[5]");


        }
    }
}
