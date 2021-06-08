namespace mana.syntax
{
    using Sprache;

    public class CoalescingExpressionSyntax : ExpressionSyntax, IPositionAware<CoalescingExpressionSyntax>
    {
        public ExpressionSyntax First { get; set; }
        public ExpressionSyntax Second { get; set; }

        public CoalescingExpressionSyntax(ExpressionSyntax f1, ExpressionSyntax f2)
        {
            this.First = f1;
            this.Second = f2;
        }

        public CoalescingExpressionSyntax(IOption<(ExpressionSyntax x, ExpressionSyntax z)> pair)
            => (First, Second) = pair.GetOrDefault();

        public new CoalescingExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
