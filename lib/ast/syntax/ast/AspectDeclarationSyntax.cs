namespace vein.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class AspectDeclarationSyntax : MemberDeclarationSyntax, IAdvancedPositionAware<AspectDeclarationSyntax>
    {
        public static readonly string GET_USAGES_METHOD_NAME = "getUsages";

        public List<ParameterSyntax> Args { get; set; } = new();
        public IdentifierExpression Identifier { get; set; }

        public new AspectDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public AspectDeclarationSyntax WithHead(MemberDeclarationSyntax head)
        {
            Aspects.AddRange(head.Aspects);
            Modifiers.AddRange(head.Modifiers);
            return this;
        }
    }
}
