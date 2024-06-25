namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class ParameterSyntax(TypeSyntax type, IdentifierExpression identifier)
    : BaseSyntax, IPositionAware<ParameterSyntax>
{
    public ParameterSyntax(IdentifierExpression type, IdentifierExpression identifier)
        : this(new TypeSyntax(type), identifier)
    {
    }

    public override SyntaxType Kind => SyntaxType.Parameter;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Type);

    public List<ModificatorSyntax> Modifiers { get; set; } = new();

    public TypeSyntax Type { get; set; } = type;

    public IdentifierExpression Identifier { get; set; } = identifier;
    
    public new ParameterSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
