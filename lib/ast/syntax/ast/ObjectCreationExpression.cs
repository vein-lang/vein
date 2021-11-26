namespace vein.syntax
{
    using System.Collections.Generic;
    using extensions;
    using Sprache;

    public class ObjectCreationExpression : ExpressionSyntax, IPositionAware<ObjectCreationExpression>
    {
        public readonly IEnumerable<ArgumentExpression> Args;

        public ObjectCreationExpression(IEnumerable<ArgumentExpression> args) => Args = args;

        public override IEnumerable<BaseSyntax> ChildNodes => Args.EmptyIfNull();

        public new ObjectCreationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
