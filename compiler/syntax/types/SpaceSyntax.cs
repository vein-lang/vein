namespace wave.syntax
{
    public class SpaceSyntax : DirectiveSyntax
    {
        public LiteralExpressionSyntax Value { get; set; }

        public override DirectiveType DirectiveKind { get; } = DirectiveType.Space;
    }
}