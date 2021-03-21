namespace wave.syntax
{
    using System.Collections.Generic;

    public abstract class DirectiveSyntax : BaseSyntax
    {
        public override SyntaxType Kind { get; } = SyntaxType.DirectiveDeclaration;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        public abstract DirectiveType DirectiveKind { get; }
        public LiteralExpressionSyntax Value { get; set; }
    }
}