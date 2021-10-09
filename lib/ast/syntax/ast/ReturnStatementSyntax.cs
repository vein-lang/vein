namespace vein.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class ReturnStatementSyntax : StatementSyntax, IPositionAware<ReturnStatementSyntax>
    {
        public ReturnStatementSyntax(ExpressionSyntax e) => Expression = e;
        public override SyntaxType Kind => SyntaxType.ReturnStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public ExpressionSyntax Expression { get; set; }

        public new ReturnStatementSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
