namespace mana.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class AspectDeclarationSyntax : MemberDeclarationSyntax, IAdvancedPositionAware<AspectDeclarationSyntax>
    {
        public List<ExpressionSyntax> Args { get; } = new();
        public string Name { get; protected set; }

        public new AspectDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
