namespace wave.syntax
{
    using Sprache;

    public class InvalidBinaryExpressionSyntax : BinaryExpressionSyntax, IPositionAware<BinaryExpressionSyntax>
    {
        public new InvalidBinaryExpressionSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}