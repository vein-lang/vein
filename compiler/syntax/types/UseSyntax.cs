namespace wave.syntax
{
    public class UseSyntax : DirectiveSyntax
    {
        public LiteralExpressionSyntax Value { get; set; }
        
        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitUseDirective(this);

        public override DirectiveType DirectiveKind { get; } = DirectiveType.Use;
    }
}