namespace vein.syntax
{
    using Sprache;

    public class IndexerArgument : ExpressionSyntax, IPositionAware<IndexerArgument>
    {
        public IdentifierExpression Identifier { get; set; }
        public ExpressionSyntax Value { get; set; }

        public IndexerArgument(IOption<IdentifierExpression> id, ExpressionSyntax value)
        {
            this.Identifier = id.GetOrDefault();
            this.Value = value;
        }

        public new IndexerArgument SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
