namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using extensions;
    using Sprache;

    public class ArrayCreationExpression : BinaryExpressionSyntax, IPositionAware<ArrayCreationExpression>
    {
        public TypeExpression Type
        {
            get => (TypeExpression)base.Left;
            set => base.Left = value;
        }

        public ArrayInitializerExpression Initializer
        {
            get => (ArrayInitializerExpression)base.Right;
            set => base.Right = value;
        }

        public ArrayCreationExpression(TypeExpression type, ArrayInitializerExpression init)
            => (Type, Initializer, OperatorType) = (type, init, ExpressionType.NewArrayInit);

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
