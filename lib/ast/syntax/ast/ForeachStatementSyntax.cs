namespace vein.syntax;

using System.Collections.Generic;
using Sprache;

public class ForeachStatementSyntax(
    LocalVariableDeclaration declaration,
    ExpressionSyntax exp,
    StatementSyntax statement)
    : StatementSyntax, IPositionAware<ForeachStatementSyntax>
{
    public LocalVariableDeclaration Variable { get; } = declaration;
    public ExpressionSyntax Expression { get; } = exp;
    public StatementSyntax Statement { get; } = statement;


    public override SyntaxType Kind => SyntaxType.ForEachStatement;

    public override IEnumerable<BaseSyntax> ChildNodes => new List<BaseSyntax> { Variable, Expression, Statement };

    public new ForeachStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
