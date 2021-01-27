namespace wave.syntax
{
    using System.Collections.Generic;

    public class DeleteStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.DeleteStatement;

        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitDeleteStatement(this);

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}