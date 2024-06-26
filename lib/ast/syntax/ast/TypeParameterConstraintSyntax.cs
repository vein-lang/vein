namespace vein.syntax;

using runtime;
using Sprache;

public class TypeParameterConstraintSyntax(TypeExpression genericIndex, TypeExpression constraint) : BaseSyntax, IPositionAware<TypeParameterConstraintSyntax>
{
    public TypeExpression GenericIndex { get; } = genericIndex;
    public TypeExpression Constraint { get; } = constraint;
    public override SyntaxType Kind => SyntaxType.TypeParameterConstraint;
    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes([GenericIndex, Constraint]);
    public new TypeParameterConstraintSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }

    public bool IsBittable => Constraint.ExpressionString.Equals("bittable");
    public bool IsClass => Constraint.ExpressionString.Equals("class");

    public VeinBaseConstraint ToConstraint(Func<TypeExpression, VeinClass> classSelector)
    {
        if (IsBittable)
            return new VeinBaseConstraintConstBittable();
        if (IsClass)
            return new VeinBaseConstraintConstClass();
        return new VeinBaseConstraintConstType(classSelector(constraint));
    }
}

