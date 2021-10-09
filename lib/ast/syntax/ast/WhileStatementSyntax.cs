namespace vein.syntax
{
    using System.Collections.Generic;

    public class WhileStatementSyntax : StatementSyntax
    {
        public WhileStatementSyntax(ExpressionSyntax e, StatementSyntax s)
            => (Expression, Statement) = (e, s);

        public override SyntaxType Kind => SyntaxType.WhileStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression, Statement);

        public ExpressionSyntax Expression { get; set; }

        public StatementSyntax Statement { get; set; }
    }
}
