namespace vein.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public abstract class DirectiveSyntax : BaseSyntax, IPositionAware<DirectiveSyntax>
    {
        public override SyntaxType Kind { get; } = SyntaxType.DirectiveDeclaration;
        public override IEnumerable<BaseSyntax> ChildNodes => new BaseSyntax[] { Value, this };
        public abstract DirectiveType DirectiveKind { get; }
        public LiteralExpressionSyntax Value { get; set; }

        public new DirectiveSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
