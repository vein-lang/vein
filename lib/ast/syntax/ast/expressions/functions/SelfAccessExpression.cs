namespace vein.syntax;

using Sprache;

public class SelfAccessExpression : ExpressionSyntax, IPositionAware<SelfAccessExpression>
{
    public new SelfAccessExpression SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }

    public override string ToString() => "self";
}
