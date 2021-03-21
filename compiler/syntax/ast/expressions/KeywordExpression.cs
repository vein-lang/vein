namespace wave.syntax
{
    using Sprache;

    public class KeywordExpression : ExpressionSyntax, IPositionAware<KeywordExpression>
    {
        public string Keyword { get; set; }


        public KeywordExpression(string keyword) : base(keyword) => this.Keyword = keyword;

        public new KeywordExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}