namespace wave.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class Unnamed01ExpressionSyntax : ExpressionSyntax, IPositionAware<Unnamed01ExpressionSyntax>
    {
        public ExpressionSyntax cc;
        public IEnumerable<ExpressionSyntax> bk1;

        public Unnamed01ExpressionSyntax(ExpressionSyntax cc, IEnumerable<ExpressionSyntax> bk1)
        {
            this.cc = cc;
            this.bk1 = bk1;
        }

        public new Unnamed01ExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}