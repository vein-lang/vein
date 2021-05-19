namespace mana.syntax
{
    using System.Collections.Generic;

    public class IfStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.IfStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression, ThenStatement, ElseStatement);

        public ExpressionSyntax Expression { get; set; }

        public StatementSyntax ThenStatement { get; set; }

        public StatementSyntax ElseStatement { get; set; }
    }
}