namespace mana.syntax
{
    using Sprache;

    public class AsTypePatternExpression : ExpressionSyntax, IPositionAware<AsTypePatternExpression>
    {
        public TypeExpression Type { get; set; }

        public AsTypePatternExpression(TypeExpression t) => this.Type = t;

        public new AsTypePatternExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
