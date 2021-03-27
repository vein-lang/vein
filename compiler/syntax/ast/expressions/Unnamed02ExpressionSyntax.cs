namespace insomnia.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class Unnamed02ExpressionSyntax : ExpressionSyntax, IPositionAware<Unnamed02ExpressionSyntax>
    {
        public Unnamed02ExpressionSyntax(ExpressionSyntax pe, IEnumerable<ExpressionSyntax> bk, IEnumerable<ExpressionSyntax> dd)
        {
            this.ExpressionString = pe.ExpressionString;
            Pe = pe;
            Bk = bk;
            Dd = dd;
        }

        public ExpressionSyntax Pe { get; }
        public IEnumerable<ExpressionSyntax> Bk { get; }
        public IEnumerable<ExpressionSyntax> Dd { get; }

        public new Unnamed02ExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }


    }
}