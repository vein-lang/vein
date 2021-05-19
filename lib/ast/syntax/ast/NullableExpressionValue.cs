namespace mana.syntax
{
    using Sprache;

    public class NullableExpressionValue : ExpressionSettingSyntax, IPositionAware<NullableExpressionValue>
    {
        public NullableExpressionValue(bool value) => this.HasNullable = value;
        public bool HasNullable { get; set; }
        public new NullableExpressionValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}