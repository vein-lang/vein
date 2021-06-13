namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class ParametersArrayExpression : ExpressionSyntax, IPositionAware<ParametersArrayExpression>
    {
        public ExpressionSyntax[] FillArgs { get; }
        public ParametersArrayExpression(IEnumerable<ExpressionSyntax> fillArgs) => FillArgs = fillArgs.EmptyIfNull().TrimNull().ToArray();

        public new ParametersArrayExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
