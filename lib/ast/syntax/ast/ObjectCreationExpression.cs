namespace vein.syntax
{
    using System.Collections.Generic;
    using extensions;
    using Sprache;

    public class ObjectCreationExpression(ArgumentListExpression args)
        : ExpressionSyntax, IPositionAware<ObjectCreationExpression>
    {
        public readonly ArgumentListExpression Args = args;

        public override IEnumerable<BaseSyntax> ChildNodes => [Args];

        public new ObjectCreationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
