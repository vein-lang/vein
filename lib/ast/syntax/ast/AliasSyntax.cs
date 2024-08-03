namespace vein.syntax;

using Sprache;


public record TypeOrMethod(TypeExpression type, MethodDeclarationSyntax method);

public class AliasSyntax(bool isGlobal, IdentifierExpression aliasName, List<TypeExpression> generics, TypeOrMethod body) : BaseSyntax, IPositionAware<AliasSyntax>
{
    public bool IsGlobal { get; } = isGlobal;
    public IdentifierExpression AliasName { get; } = aliasName;
    public List<TypeExpression> Generics { get; } = generics;
    public TypeExpression? Type { get; } = body.type;
    public MethodDeclarationSyntax? MethodDeclaration { get; } = body.method;

    public bool IsMethod => MethodDeclaration != null;
    public bool IsType => Type != null;

    public override SyntaxType Kind => SyntaxType.Alias;
    public override IEnumerable<BaseSyntax> ChildNodes
        => GetNodes([AliasName, Type, MethodDeclaration]);

    public AliasSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
