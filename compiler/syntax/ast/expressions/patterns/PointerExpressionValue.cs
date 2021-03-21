namespace wave.syntax
{
    using Sprache;

    public class PointerExpressionValue : ExpressionSettingSyntax, IPositionAware<PointerExpressionValue>
    {
        public PointerExpressionValue(bool value) => this.HasPointer = value;
        public bool HasPointer { get; set; }
        public new PointerExpressionValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}