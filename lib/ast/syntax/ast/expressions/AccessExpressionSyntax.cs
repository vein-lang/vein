namespace wave.syntax
{
    using Sprache;

    public class AccessExpressionSyntax : BinaryExpressionSyntax, IPositionAware<AccessExpressionSyntax>
    {
        public ExpressionSyntax Exp1, Exp2;
        public ExpressionSyntax op;
        public new AccessExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}