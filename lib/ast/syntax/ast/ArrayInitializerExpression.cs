namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using extensions;
    using Sprache;

    public class ArrayCreationExpression : ExpressionSyntax, IPositionAware<ArrayCreationExpression>
    {
        public TypeExpression Type { get; set; }

        public ArrayInitializerExpression Initializer { get; set; }

        public ArrayCreationExpression(TypeExpression type, ArrayInitializerExpression init)
            => (Type, Initializer) = (type, init);

        public new ArrayCreationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }

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
