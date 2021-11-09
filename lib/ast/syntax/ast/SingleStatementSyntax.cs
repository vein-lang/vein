namespace vein.syntax;

using Sprache;

public class SingleStatementSyntax : StatementSyntax, IAdvancedPositionAware<SingleStatementSyntax>
{
    public ExpressionSyntax Expression { get; }
    public SingleStatementSyntax(ExpressionSyntax exp)
    {
        this.Expression = exp;
        SetStart(exp.Transform.pos).SetEnd(exp.Transform.pos);
    }

    public new SingleStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
