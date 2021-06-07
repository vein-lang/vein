namespace mana.syntax
{
    using Sprache;

    public class UseSyntax : DirectiveSyntax, IPositionAware<UseSyntax>
    {
        public override DirectiveType DirectiveKind { get; } = DirectiveType.Use;

        public new UseSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}