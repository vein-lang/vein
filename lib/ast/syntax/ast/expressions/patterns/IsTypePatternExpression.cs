namespace mana.syntax
{
    using Sprache;

    public class IsTypePatternExpression : ExpressionSyntax, IPositionAware<IsTypePatternExpression>
    {
        public TypeExpression Type { get; set; }

        public IsTypePatternExpression(TypeExpression t) => this.Type = t;

        public new IsTypePatternExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
