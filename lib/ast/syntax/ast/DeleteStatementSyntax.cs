namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class DeleteStatementSyntax(ExpressionSyntax e)
    : StatementSyntax, IAdvancedPositionAware<DeleteStatementSyntax>
{
    public override SyntaxType Kind => SyntaxType.DeleteStatement;

    public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

    public ExpressionSyntax Expression { get; set; } = e;

    public new DeleteStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
