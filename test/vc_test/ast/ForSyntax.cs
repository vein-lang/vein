namespace wc_test.ast
{
    using System.Linq;
    using vein.stl;
    using vein.syntax;
    using Sprache;
    using NUnit.Framework;

    public class ForSyntax_test
    {
        public static ManaSyntax Mana => new();


        [Test]
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

        [Test]
        public void ForeachParseTest2()
        {
            var result = Mana.foreach_statement.End().ParseMana("foreach (auto i in Sobbaka) { return 1; }");

            Assert.False(result.IsBrokenToken);

            var @foreach = IshtarAssert.IsType<ForeachStatementSyntax>(result);

            Assert.AreEqual(SyntaxType.ForEachStatement, @foreach.Kind);

            var variable = IshtarAssert.IsType<LocalVariableDeclaration>(@foreach.Variable);

            Assert.AreEqual("i", variable.Identifier.ExpressionString);

            var exp = IshtarAssert.IsType<IdentifierExpression>(@foreach.Expression);

            Assert.AreEqual("Sobbaka", exp.ExpressionString);

            var statement = IshtarAssert.IsType<BlockSyntax>(@foreach.Statement);
            var retStatement = IshtarAssert.IsType<ReturnStatementSyntax>(statement.Statements.Single());
        }
    }
}
