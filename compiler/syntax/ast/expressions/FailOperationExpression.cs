namespace wave.syntax
{
    using Sprache;

    public class FailOperationExpression : UnaryExpressionSyntax, IPositionAware<FailOperationExpression>
    {
        public FailOperationExpression(ExpressionSyntax expression) => this.Operand = expression;

        public override SyntaxType Kind => SyntaxType.FailStatement;
        
        public new FailOperationExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}