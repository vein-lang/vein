namespace vein.syntax
{
    using System;
    using System.Linq;
    using Sprache;
    using stl;

    [Obsolete]
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
