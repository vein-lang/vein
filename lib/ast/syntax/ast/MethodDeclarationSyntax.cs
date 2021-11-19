namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using vein.extensions;

    public class MethodDeclarationSyntax : MemberDeclarationSyntax
    {
        public MethodDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Method;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(ReturnType)).Concat(Parameters).Concat(GetNodes(Body)).Where(n => n != null);

        public TypeSyntax ReturnType { get; set; }

        public IdentifierExpression Identifier { get; set; }

        public List<ParameterSyntax> Parameters { get; set; } = new();

        public BlockSyntax Body { get; set; }

        public bool IsAbstract => Body == null;

        public string GetQualityName()
            => $"{Identifier}({Parameters.Select(x => $"{(x.Type.IsSelf ? OwnerClass.Identifier : x.Type.Identifier)}").Join(",")})";

        public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
        {
            ReturnType = typeAndName.Type;
            Identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
            return this;
        }

        public override MemberDeclarationSyntax WithName(IdentifierExpression name)
        {
            Identifier = name;
            return this;
        }

        public bool IsConstructor() => Identifier.ExpressionString.Equals("new");

        public ClassDeclarationSyntax OwnerClass { get; set; }
    }
}
