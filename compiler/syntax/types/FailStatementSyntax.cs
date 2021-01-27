namespace wave.syntax
{
    using System.Collections.Generic;

    public class FailStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.ThrowStatement;

        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitThrowStatement(this);

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}