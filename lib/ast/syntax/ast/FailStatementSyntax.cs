namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class FailStatementSyntax(ExpressionSyntax e) : StatementSyntax, IAdvancedPositionAware<FailStatementSyntax>
{
    public override SyntaxType Kind => SyntaxType.FailStatement;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

    public ExpressionSyntax Expression { get; set; } = e;

    public new FailStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
