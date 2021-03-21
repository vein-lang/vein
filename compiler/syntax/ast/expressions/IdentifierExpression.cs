namespace wave.syntax
{
    using Sprache;

    public class IdentifierExpression : ExpressionSyntax, IPositionAware<IdentifierExpression>
    {
        public IdentifierExpression(string name) : base(name) 
            => this.ExpressionString = name;

        public new IdentifierExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}