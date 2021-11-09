namespace vein.syntax;

public class InterfaceDeclarationSyntax : ClassDeclarationSyntax
{
    public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading = null)
        : base(heading)
    {
    }

    public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody)
        : base(heading, classBody)
    {
    }

    public override SyntaxType Kind => SyntaxType.Interface;

    public override bool IsInterface => true;
}
