namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class ArrayInitializerExpression : ExpressionSyntax, IPositionAware<ArrayInitializerExpression>
    {
        public readonly ExpressionSyntax[] Sizes;
        public readonly ParametersArrayExpression Args;

        public ArrayInitializerExpression(IEnumerable<ExpressionSyntax> sizes, IOption<ParametersArrayExpression> args)
        {
            Args = args.GetOrDefault();
            Sizes = sizes.ToArray();
        }

        public override IEnumerable<BaseSyntax> ChildNodes => Sizes.EmptyIfNull().Concat(new[] { Args }).TrimNull();

        public new ArrayInitializerExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
