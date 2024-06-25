namespace vein.syntax;

using System.Collections.Generic;
using System.Linq;

public class PropertyDeclarationSyntax(MemberDeclarationSyntax? heading = null) : MemberDeclarationSyntax(heading)
{
    public PropertyDeclarationSyntax(IEnumerable<AccessorDeclarationSyntax> accessors, MemberDeclarationSyntax? heading = null)
        : this(heading) =>
        Accessors = accessors.ToList();

    public override SyntaxType Kind => SyntaxType.Property;

    public override IEnumerable<BaseSyntax> ChildNodes =>
        base.ChildNodes.Concat(GetNodes(Type, Getter, Setter, Expression));

    public TypeSyntax Type { get; set; }

    public IdentifierExpression Identifier { get; set; }

    public List<AccessorDeclarationSyntax> Accessors { get; set; } = new();

    public AccessorDeclarationSyntax? Getter => Accessors.FirstOrDefault(a => a.IsGetter);

    public AccessorDeclarationSyntax? Setter => Accessors.FirstOrDefault(a => a.IsSetter);

    public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
    {
        Type = typeAndName.Type;
        Identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
        return this;
    }
    public ExpressionSyntax? Expression { get; set; }

    public bool IsShortform() => Getter is null && Setter is null && Expression is not null;

    public ClassDeclarationSyntax OwnerClass { get; set; }
}
