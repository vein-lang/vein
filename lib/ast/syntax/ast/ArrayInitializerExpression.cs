namespace mana.syntax
{
    using System.Collections.Generic;
    using extensions;
    using Sprache;

    public class ArrayInitializerExpression : ExpressionSyntax, IPositionAware<ArrayInitializerExpression>
    {
        public readonly IEnumerable<ExpressionSyntax> Args;

        public ArrayInitializerExpression(IEnumerable<ExpressionSyntax> args) => Args = args;

        public override IEnumerable<BaseSyntax> ChildNodes => Args.EmptyIfNull();

        public new ArrayInitializerExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
