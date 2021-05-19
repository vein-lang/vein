namespace mana.syntax
{
    using Sprache;

    public class PointerSpecifierValue : ExpressionSettingSyntax, IPositionAware<PointerSpecifierValue>
    {
        public PointerSpecifierValue(bool value) => this.HasPointer = value;
        public bool HasPointer { get; set; }
        public new PointerSpecifierValue SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}