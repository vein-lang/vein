namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using stl;

    public class FieldDeclarationSyntax : MemberDeclarationSyntax
    {
        public FieldDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Field;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Type)).Concat(new List<BaseSyntax> { Field }).Where(n => n != null);

        public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
        {
            Type = typeAndName.Type;

            var identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
            Field.Identifier = identifier;

            return this;
        }

        public override MemberDeclarationSyntax WithName(IdentifierExpression name)
        {
            Field.Identifier = name;
            return this;
        }

        public TypeSyntax Type { get; set; }

        public FieldDeclaratorSyntax Field { get; set; }

        public ClassDeclarationSyntax OwnerClass { get; set; }
    }
}
