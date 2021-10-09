namespace vein.syntax
{
    using System.Collections.Generic;

    public class DeleteStatementSyntax : StatementSyntax
    {
        public DeleteStatementSyntax(ExpressionSyntax e) => Expression = e;
        public override SyntaxType Kind => SyntaxType.DeleteStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}
