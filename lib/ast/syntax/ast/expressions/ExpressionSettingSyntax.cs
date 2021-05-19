namespace mana.syntax
{
    using Sprache;

    public abstract class ExpressionSettingSyntax : ExpressionSyntax, IPositionAware<ExpressionSettingSyntax>
    {
        public new ExpressionSettingSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}