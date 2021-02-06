namespace wave.syntax
{
    public class UseSyntax : DirectiveSyntax
    {
        public LiteralExpressionSyntax Value { get; set; }

        public override DirectiveType DirectiveKind { get; } = DirectiveType.Use;
    }
}