namespace wave.syntax
{
    using Sprache;

    public class BracketExpression : ExpressionSyntax, IPositionAware<BracketExpression>
    {
        public NullableExpressionValue Nullable { get; set; }
        public IndexerArgument[] Arguments { get; set; }

        public BracketExpression(IOption<NullableExpressionValue> nullable, IndexerArgument[] args)
        {
            this.Nullable = nullable.GetOrElse(new NullableExpressionValue(false));
            this.Arguments = args;
        }
        public new BracketExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}