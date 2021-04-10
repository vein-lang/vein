namespace insomnia.syntax
{
    using Sprache;

    public class LocalVariableDeclaration : StatementSyntax, IPositionAware<LocalVariableDeclaration>
    {
        public readonly IdentifierExpression Identifier;
        public new readonly IOption<ExpressionSyntax> Body;

        public LocalVariableDeclaration(IdentifierExpression id, IOption<ExpressionSyntax> body)
        {
            Identifier = id;
            Body = body;
        }

        public new LocalVariableDeclaration SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}