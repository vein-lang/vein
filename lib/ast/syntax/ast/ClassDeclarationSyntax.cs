namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using extensions;
    using stl;

    public class ClassDeclarationSyntax : MemberDeclarationSyntax
    {
        public ClassDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public ClassDeclarationSyntax(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody)
            : this(heading)
        {
            Identifier = classBody.Identifier;
            IsInterface = classBody.IsInterface;
            IsStruct = classBody.IsStruct;
            Inheritances = classBody.Inheritances;
            Members = classBody.Members;
            InnerComments = classBody.InnerComments;
            TrailingComments = classBody.TrailingComments;
        }

        public static ClassDeclarationSyntax Create(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody) =>
            classBody.IsInterface ? new InterfaceDeclarationSyntax(heading, classBody) :
                new ClassDeclarationSyntax(heading, classBody);

        public override SyntaxType Kind => SyntaxType.Class;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(Inheritances).Concat(Members).Where(n => n != null);

        public IdentifierExpression Identifier { get; set; }

        public virtual bool IsInterface { get; set; }
        public virtual bool IsStruct { get; set; }

        public List<TypeSyntax> Inheritances { get; set; } = new();
        
        public List<string> InnerComments { get; set; } = new();

        public List<MemberDeclarationSyntax> Members { get; set; } = new();
        
        public List<MethodDeclarationSyntax> Methods => Members.OfExactType<MethodDeclarationSyntax>().ToList();

        public List<FieldDeclarationSyntax> Fields => Members.OfType<FieldDeclarationSyntax>().ToList();

        public List<PropertyDeclarationSyntax> Properties => Members.OfType<PropertyDeclarationSyntax>().ToList();

        public DocumentDeclaration OwnerDocument { get; set; }
    }
}