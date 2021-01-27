namespace wave.syntax
{
    using System.Collections.Generic;

    public class ReturnStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.ReturnStatement;

        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitReturnStatement(this);

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}