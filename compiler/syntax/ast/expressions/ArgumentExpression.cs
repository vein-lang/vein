namespace wave.syntax
{
    using Sprache;

    public class ArgumentExpression : ExpressionSyntax, IPositionAware<ArgumentExpression>
    {
        public IdentifierExpression Identifier { get; set; }
        public ExpressionSyntax Type { get; set; }
        public ExpressionSyntax Value { get; set; }

        public ArgumentExpression(IOption<IdentifierExpression> id, IOption<ExpressionSyntax> t, ExpressionSyntax v)
        {
            this.Identifier = id.GetOrDefault();
            this.Type = t.GetOrDefault();
            this.Value = v;
        }
        public new ArgumentExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}