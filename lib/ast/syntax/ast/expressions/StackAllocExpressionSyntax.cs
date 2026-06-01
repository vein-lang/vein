namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class StackAllocExpressionSyntax(TypeSyntax elementType, ExpressionSyntax count)
    : ExpressionSyntax("stackalloc"), IPositionAware<StackAllocExpressionSyntax>
{
    public override SyntaxType Kind => SyntaxType.StackAllocExpression;

public TypeSyntax ElementType { get; } = elementType;
public ExpressionSyntax Count { get; } = count;

public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(ElementType, Count);

    public new StackAllocExpressionSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
