namespace wc_test.ast
{
    using System.Linq;
    using mana.stl;
    using mana.syntax;
    using Sprache;
    using Xunit;
    using Xunit.Abstractions;

    public class ForSyntax_test
    {
        private readonly ITestOutputHelper _logger;

        public ForSyntax_test(ITestOutputHelper logger)
        {
            _logger = logger;
            ManaParserExtensions._log = s => _logger.WriteLine(s);
        }

        public static ManaSyntax Mana => new();


        [Fact]
        public void ForeachParseTest1()
        {
            Mana.KeywordExpression("foreach").Positioned().Token().End().ParseMana("foreach");
            Mana.local_variable_declaration.Positioned().Token().End().ParseMana("auto i ");
            Mana.KeywordExpression("in").Positioned().Token().End().ParseMana("in");
            Mana.QualifiedExpression.Positioned().Token().End().ParseMana("Sobbaka");
            Mana.embedded_statement.Token().Positioned().End().ParseMana("{ return 1; }");


            var result = Mana.foreach_statement.End().ParseMana("foreach (auto i in Sobbaka) { return 1; }");

            Assert.False(result.IsBrokenToken);
        }

        [Fact]
        public void ForeachParseTest2()
        {
            var result = Mana.foreach_statement.End().ParseMana("foreach (auto i in Sobbaka) { return 1; }");

            Assert.False(result.IsBrokenToken);

            var @foreach = Assert.IsType<ForeachStatementSyntax>(result);

            Assert.Equal(SyntaxType.ForEachStatement, @foreach.Kind);

            var variable = Assert.IsType<LocalVariableDeclaration>(@foreach.Variable);

            Assert.Equal("i", variable.Identifier.ExpressionString);

            var exp = Assert.IsType<IdentifierExpression>(@foreach.Expression);

            Assert.Equal("Sobbaka", exp.ExpressionString);

            var statement = Assert.IsType<BlockSyntax>(@foreach.Statement);
            var retStatement = Assert.IsType<ReturnStatementSyntax>(statement.Statements.Single());
        }
    }
}
