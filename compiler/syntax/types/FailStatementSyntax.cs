namespace wave.syntax
{
    using System.Collections.Generic;

    public class FailStatementSyntax : StatementSyntax
    {
        public override SyntaxType Kind => SyntaxType.ThrowStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }
    }
}