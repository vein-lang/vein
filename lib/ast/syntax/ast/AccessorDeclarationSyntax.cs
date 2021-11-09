namespace vein.syntax;

using System.Collections.Generic;

public class AccessorDeclarationSyntax : MemberDeclarationSyntax
{
    public AccessorDeclarationSyntax(MemberDeclarationSyntax heading = null)
        : base(heading)
    {
    }

    public override SyntaxType Kind => SyntaxType.Accessor;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Body);

    public bool IsGetter { get; set; }

    public bool IsSetter => !IsGetter;

    public BlockSyntax Body { get; set; }

    public bool IsEmpty => Body == null;
}
