namespace mana.syntax
{
    using System.Linq;
    using extensions;
    using Sprache;

    public class TypeExpression : ExpressionSyntax, IPositionAware<TypeExpression>
    {
        public TypeSyntax Typeword { get; set; }

        public TypeExpression(TypeSyntax typeword) : base(typeword.Identifier.ExpressionString)
            => this.Typeword = typeword;

        public new TypeExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public TypeExpression WithMetadata(ExpressionSettingSyntax[] settings)
        {
            Typeword.PointerRank = settings.OfExactType<PointerSpecifierValue>().Count(x => x.HasPointer);
            Typeword.ArrayRank = settings.OfExactType<RankSpecifierValue>().Sum(x => x.Rank);
            return this;
        }
    }
}