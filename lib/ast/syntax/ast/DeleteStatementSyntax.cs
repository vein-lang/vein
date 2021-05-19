namespace mana.syntax
{
    using System.Collections.Generic;

    public class DeleteStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.DeleteStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}