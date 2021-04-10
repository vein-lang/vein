namespace insomnia.syntax
{
    using Sprache;

    public class AnonymousFunctionExpressionSyntax : ExpressionSyntax, IPositionAware<AnonymousFunctionExpressionSyntax>
    {
        public ExpressionSyntax Declaration { get; set; }
        public ExpressionSyntax Body { get; set; }

        public AnonymousFunctionExpressionSyntax(ExpressionSyntax dec, ExpressionSyntax body)
        {
            this.Declaration = dec;
            this.Body = body;
        }

        public new AnonymousFunctionExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}