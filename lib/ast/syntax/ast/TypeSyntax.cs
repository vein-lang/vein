namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;
    using stl;

    public class TypeSyntax : BaseSyntax, IPositionAware<TypeSyntax>
    {
        public TypeSyntax(IEnumerable<IdentifierExpression> qualifiedName)
        {
            Namespaces = qualifiedName.ToList();

            if (Namespaces.Count <= 0)
                return;
            var lastItem = Namespaces.Count - 1;
            Identifier = Namespaces[lastItem];
            Namespaces.RemoveAt(lastItem);
            Transform = new Transform(new Position(0, 0, 0), 0);
        }

        public TypeSyntax(params IdentifierExpression[] qualifiedName)
            : this(qualifiedName.AsEnumerable())
        {
        }

        public TypeSyntax(TypeSyntax template)
        {
            Namespaces = template.Namespaces;
            Identifier = template.Identifier;
            TypeParameters = template.TypeParameters;
        }

        public override SyntaxType Kind => SyntaxType.Type;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            TypeParameters.Where(n => n != null);

        public List<IdentifierExpression> Namespaces { get; set; }

        public IdentifierExpression Identifier { get; set; }

        public List<TypeSyntax> TypeParameters { get; set; }
        public bool IsArray { get; set; }
        public bool IsPointer { get; set; }


        public int ArrayRank { get; set; }
        public int PointerRank { get; set; }

        public string GetFullName()
        {
            var result = $"global::";
            if (Namespaces.Any())
                result = $"{result}{Namespaces.Select(x => x.ExpressionString).Join("/")}/";
            result = $"{result}{Identifier}";
            if (IsPointer)
                result = $"{result}{new string('*', PointerRank)}";
            if (IsArray)
                result = $"{result}[{new string(',', ArrayRank)}]";
            return result;
        }

        TypeSyntax IPositionAware<TypeSyntax>.SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}