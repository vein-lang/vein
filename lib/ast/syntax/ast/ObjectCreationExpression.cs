namespace mana.syntax
{
    using System.Collections.Generic;
    using extensions;
    using Sprache;

    public class ObjectCreationExpression : ExpressionSyntax, IPositionAware<ObjectCreationExpression>
    {
        public readonly IEnumerable<ExpressionSyntax> Args;

        public ObjectCreationExpression(IEnumerable<ExpressionSyntax> args) => Args = args;

        public override IEnumerable<BaseSyntax> ChildNodes => Args.EmptyIfNull();

        public new ObjectCreationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
