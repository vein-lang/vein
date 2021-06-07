namespace mana.syntax
{
    using System.Linq;
    using Sprache;
    using stl;

    public class MethodInvocationExpression : ExpressionSyntax, IPositionAware<MethodInvocationExpression>
    {
        public ExpressionSyntax[] Arguments { get; set; }

        public MethodInvocationExpression(IOption<ExpressionSyntax[]> args)
            => this.Arguments = args.GetOrEmpty().ToArray();

        public new MethodInvocationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}