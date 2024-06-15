namespace vein.syntax
{
    using Sprache;

    public class LocalVariableDeclaration : StatementSyntax, IAdvancedPositionAware<LocalVariableDeclaration>
    {
        public readonly IdentifierExpression Identifier;
        public readonly IOption<ExpressionSyntax> Body;

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
