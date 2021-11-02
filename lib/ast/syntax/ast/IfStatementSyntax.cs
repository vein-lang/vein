namespace vein.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class IfStatementSyntax : StatementSyntax, IPositionAware<IfStatementSyntax>
    {
        public IfStatementSyntax(ExpressionSyntax e, StatementSyntax then, StatementSyntax @else)
            => (Expression, ThenStatement, ElseStatement) = (e, then, @else);
        public override SyntaxType Kind => SyntaxType.IfStatement;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression, ThenStatement, ElseStatement);

        public ExpressionSyntax Expression { get; set; }

        public StatementSyntax ThenStatement { get; set; }

        public StatementSyntax ElseStatement { get; set; }

        public new IfStatementSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
