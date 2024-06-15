namespace vein.syntax;

using Sprache;

public class ForStatementSyntax(
    IOption<LocalVariableDeclaration> loopVariable,
    IOption<ExpressionSyntax> loopContact,
    IOption<ExpressionSyntax> loopCounter,
    StatementSyntax statement)
    : StatementSyntax, IPositionAware<ForStatementSyntax>
{
    public LocalVariableDeclaration? LoopVariable { get; } = loopVariable.GetOrDefault();
    public ExpressionSyntax? LoopContact { get; } = loopContact.GetOrDefault();
    public ExpressionSyntax? LoopCounter { get; } = loopCounter.GetOrDefault();
    public StatementSyntax Statement { get; } = statement;


    public override SyntaxType Kind => SyntaxType.ForEachStatement;

    public override IEnumerable<BaseSyntax> ChildNodes
        => new BaseSyntax[] { LoopVariable!, LoopContact!, LoopCounter!, Statement }.Where(x => x is not null).ToList();

    public new ForStatementSyntax SetPos(Position startPos, int length)
    {
        base.SetPos(startPos, length);
        return this;
    }
}
