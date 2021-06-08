namespace mana.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class ChainAccessExpression : ExpressionSyntax, IPositionAware<ChainAccessExpression>
    {
        public ExpressionSyntax Start;
        public IEnumerable<ExpressionSyntax> Other;

        public ChainAccessExpression(ExpressionSyntax start, IEnumerable<ExpressionSyntax> other)
        {
            this.Start = start;
            this.Other = other;
        }

        public new ChainAccessExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
