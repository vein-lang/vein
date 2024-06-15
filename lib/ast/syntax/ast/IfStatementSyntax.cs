namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class IfStatementSyntax(ExpressionSyntax e, StatementSyntax then, StatementSyntax? @else)
    : StatementSyntax, IPositionAware<IfStatementSyntax>
{
    public override SyntaxType Kind => SyntaxType.IfStatement;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression, ThenStatement, ElseStatement);

    public ExpressionSyntax Expression { get; set; } = e;

    public StatementSyntax ThenStatement { get; set; } = then;

    public StatementSyntax? ElseStatement { get; set; } = @else;

    public new IfStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
