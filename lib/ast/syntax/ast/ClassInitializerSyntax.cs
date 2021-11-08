namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;
using extensions;

public class ClassInitializerSyntax : MemberDeclarationSyntax
{
    public ClassInitializerSyntax(MemberDeclarationSyntax heading = null)
        : base(heading)
    {
    }

    public override SyntaxType Kind => SyntaxType.ClassInitializer;

    public override IEnumerable<BaseSyntax> ChildNodes =>
        base.ChildNodes.Concat(GetNodes(Body));

    public BlockSyntax Body { get; set; }

    public bool IsStatic => Modifiers.EmptyIfNull().Any(m => m.ModificatorKind == ModificatorKind.Static);
}