namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class AccessorDeclarationSyntax : MemberDeclarationSyntax, IPositionAware<AccessorDeclarationSyntax>
{
    public AccessorDeclarationSyntax(MemberDeclarationSyntax? heading = null)
        : base(heading)
    {
    }

    public override SyntaxType Kind => SyntaxType.Accessor;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Body);

    public bool IsGetter { get; set; }

    public bool IsSetter => !IsGetter;

    public BlockSyntax Body { get; set; }

    public bool IsEmpty => Body == null;

    public new AccessorDeclarationSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
