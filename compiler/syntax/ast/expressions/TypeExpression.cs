namespace wave.syntax
{
    using Sprache;

    public class TypeExpression : ExpressionSyntax, IPositionAware<TypeExpression>
    {
        public TypeSyntax Typeword { get; set; }

        public TypeExpression(TypeSyntax typeword) : base(typeword.Identifier) => this.Typeword = typeword;

        public new TypeExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public TypeExpression WithMetadata(ExpressionSettingSyntax[] settings)
        {
            return this;
        }
    }
}