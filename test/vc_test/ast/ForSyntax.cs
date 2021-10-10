namespace wc_test.ast
{
    using System.Linq;
    using vein.stl;
    using vein.syntax;
    using Sprache;
    using NUnit.Framework;

    public class ForSyntax_test
    {
        public static VeinSyntax Vein => new();


        [Test]
        public void ForeachParseTest1()
        {
            Vein.KeywordExpression("foreach").Positioned().Token().End().ParseVein("foreach");
            Vein.local_variable_declaration.Positioned().Token().End().ParseVein("auto i ");
            Vein.KeywordExpression("in").Positioned().Token().End().ParseVein("in");
            Vein.QualifiedExpression.Positioned().Token().End().ParseVein("Sobbaka");
            Vein.embedded_statement.Token().Positioned().End().ParseVein("{ return 1; }");


            var result = Vein.foreach_statement.End().ParseVein("foreach (auto i in Sobbaka) { return 1; }");

            Assert.False(result.IsBrokenToken);
        }

        [Test]
        public void ForeachParseTest2()
        {
            var result = Vein.foreach_statement.End().ParseVein("foreach (auto i in Sobbaka) { return 1; }");

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
