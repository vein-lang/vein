namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using extensions;
    using runtime;
    using Sprache;
    using stl;

    [DebuggerDisplay("{ToDebugString()}")]
    public class TypeSyntax : BaseSyntax, IPositionAware<TypeSyntax>, IEquatable<TypeSyntax>
    {
        public static TypeSyntax From(NameSymbol nameSymbol)
            => new(new IdentifierExpression(nameSymbol.name));


        public TypeSyntax(IEnumerable<IdentifierExpression> qualifiedName)
        {
            Namespaces = qualifiedName.ToList();

            if (Namespaces.Count <= 0)
                return;
            var lastItem = Namespaces.Count - 1;
            Identifier = Namespaces[lastItem];
            Namespaces.RemoveAt(lastItem);
            Transform = Identifier.Transform;
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


        private string ToDebugString()
        {
            if (!IsGeneric) return $"TypeName [{Identifier.ExpressionString}{(IsPointer ? "*" : "")}]";
            return $"TypeName [{Identifier.ExpressionString}<{string.Join(',', TypeParameters.Select(x => x.Identifier.ToString()))}>]";
        }

        public override SyntaxType Kind => SyntaxType.Type;

        public override IEnumerable<BaseSyntax> ChildNodes
        {
            get
            {
                var list = new List<BaseSyntax>();

                list.AddRange(Namespaces);
                list.AddRange(TypeParameters);
                if (Identifier is not null)
                    list.Add(Identifier);
                return list;
            }
        }

        public List<IdentifierExpression> Namespaces { get; set; }

        public IdentifierExpression Identifier { get; set; }

        public List<TypeSyntax> TypeParameters { get; set; } = new();

        public bool IsGeneric => TypeParameters.Any();
        public bool IsArray { get; set; }
        public bool IsPointer { get; set; }
        public bool IsAsyncJob => Identifier.ExpressionString.Equals("Job");


        public int ArrayRank { get; set; }
        public int PointerRank { get; set; }
        public bool IsSelf => Identifier.ToString().Equals("self");

        public static IdentifierExpression MutateClassNameWithGenerics(IdentifierExpression id, List<TypeExpression> generics)
        {
            if (!generics.Any())
                return id;
            var newName = $"{id}<{generics.Select(x => $"{new VeinTypeArg(x.Typeword.Identifier)}").Join(",")}>";
            return new IdentifierExpression(newName).SetPos<IdentifierExpression>(id.Transform);
        }

        public static string MutateClassNameWithGenerics(TypeSyntax typeName)
        {
            if (!typeName.IsGeneric)
                return typeName.Identifier;
            var newName = $"{typeName.Identifier}<{typeName.TypeParameters.Select(x => $"{new VeinTypeArg(x.Identifier)}").Join(",")}>";
            return new IdentifierExpression(newName);
        }

        public static Regex ToGlobPattern(TypeSyntax typeName)
        {
            if (!typeName.IsGeneric)
                throw new NotSupportedException();
            var newName = $@"^{typeName.Identifier}\<{typeName.TypeParameters.Select(x => $"{new VeinTypeArg(@"[^\W+]+")}").Join(@"\,")}\>";
            return new Regex(newName);
        }


        public string GetFullName()
        {
            var result = $"";
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

        public bool Equals(TypeSyntax? other)
        {
            if (other == null) return false;
            return other.Identifier.Equals(Identifier);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypeSyntax)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Namespaces, Identifier, TypeParameters, IsArray, IsPointer, ArrayRank, PointerRank);

        public VeinTypeArg ToTypeArg(IReadOnlyList<VeinBaseConstraint> constraint) =>
            new(Identifier.ExpressionString, constraint);
    }
}
