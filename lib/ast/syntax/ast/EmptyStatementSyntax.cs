namespace wave.syntax
{
    using Sprache;

    public class EmptyStatementSyntax : StatementSyntax, IPositionAware<EmptyStatementSyntax>
    {
        public new EmptyStatementSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}