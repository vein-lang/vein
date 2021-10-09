namespace vein.syntax
{
    using System.Collections.Generic;

    public class FailStatementSyntax : StatementSyntax
    {
        public FailStatementSyntax(ExpressionSyntax e) => Expression = e;

        public override SyntaxType Kind => SyntaxType.FailStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}
