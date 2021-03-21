namespace wave.syntax
{
    using System.Linq;
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
            Typeword.PointerRank = settings.OfExactType<PointerSpecifierValue>().Where(x => x.HasPointer).Count();
            Typeword.ArrayRank = settings.OfExactType<RankSpecifierValue>().Sum(x => x.Rank);
            return this;
        }
    }
}