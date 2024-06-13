namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public class TypeExpression(TypeSyntax typeword)
        : ExpressionSyntax(typeword.Identifier.ExpressionString), IPositionAware<TypeExpression>
    {
        public TypeSyntax Typeword { get; set; } = typeword;
        public override SyntaxType Kind => SyntaxType.Type;

        public new TypeExpression SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override IEnumerable<BaseSyntax> ChildNodes => Typeword.ChildNodes;

        public TypeExpression WithMetadata(ExpressionSettingSyntax[] settings)
        {
            Typeword.PointerRank = settings.OfExactType<PointerSpecifierValue>().Count(x => x.HasPointer);
            Typeword.ArrayRank = settings.OfExactType<RankSpecifierValue>().Sum(x => x.Rank);
            Typeword.IsArray = settings.Any(x => x is RankSpecifierValue);
            Typeword.IsPointer = settings.Any(x => x is PointerSpecifierValue);
            return this;
        }

        public override string ToString() => $"TypeExpression [{ExpressionString}]";
    }
}
