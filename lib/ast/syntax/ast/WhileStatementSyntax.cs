namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class WhileStatementSyntax(ExpressionSyntax e, StatementSyntax s) : StatementSyntax, IAdvancedPositionAware<WhileStatementSyntax>
{
    public override SyntaxType Kind => SyntaxType.WhileStatement;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression, Statement);

    public ExpressionSyntax Expression { get; set; } = e;

    public StatementSyntax Statement { get; set; } = s;

    public new WhileStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
