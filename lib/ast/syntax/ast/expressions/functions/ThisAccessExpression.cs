namespace vein.syntax;

using Sprache;

public class ThisAccessExpression : ExpressionSyntax, IPositionAware<ThisAccessExpression>
{
    public new ThisAccessExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}