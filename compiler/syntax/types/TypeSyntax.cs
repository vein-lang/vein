namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;
    using stl;

    public class TypeSyntax : BaseSyntax, IPositionAware<TypeSyntax>
    {
        public TypeSyntax(IEnumerable<string> qualifiedName)
        {
            Namespaces = qualifiedName.ToList();

            if (Namespaces.Count <= 0) 
                return;
            var lastItem = Namespaces.Count - 1;
            Identifier = Namespaces[lastItem];
            Namespaces.RemoveAt(lastItem);
            Transform = new Transform(new Position(0, 0, 0), 0);
        }

        public TypeSyntax(params string[] qualifiedName)
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

        public List<string> Namespaces { get; set; }

        public string Identifier { get; set; }

        public List<TypeSyntax> TypeParameters { get; set; }
        public bool IsArray { get; set; }

        public string AsString() =>
            string.Join(".", Namespaces.Concat(Enumerable.Repeat(Identifier, 1))) +
            (TypeParameters.IsNullOrEmpty() ? string.Empty :
                "<" + string.Join(", ", TypeParameters.Select(t => t.AsString())) + ">") +
            (IsArray ? "[]" : string.Empty);

        TypeSyntax IPositionAware<TypeSyntax>.SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}