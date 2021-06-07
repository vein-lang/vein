namespace mana.syntax
{
    using System.Linq;
    using Sprache;
    using stl;

    public class FullMethodInvocationExpression : ExpressionSyntax, IPositionAware<FullMethodInvocationExpression>
    {
        public ExpressionSyntax[] Arguments { get; set; }

        public FullMethodInvocationExpression(IOption<ExpressionSyntax[]> args)
            => this.Arguments = args.GetOrEmpty().ToArray();

        public new FullMethodInvocationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}