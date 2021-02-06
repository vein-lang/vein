namespace wave.syntax
{
    using System.Collections.Generic;
    using System.Linq;
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
            BaseType = classBody.BaseType;
            Interfaces = classBody.Interfaces;
            Members = classBody.Members;
            InnerComments = classBody.InnerComments;
            TrailingComments = classBody.TrailingComments;
        }

        public static ClassDeclarationSyntax Create(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody) =>
            classBody.IsInterface ? new InterfaceDeclarationSyntax(heading, classBody) :
                new ClassDeclarationSyntax(heading, classBody);

        public override SyntaxType Kind => SyntaxType.Class;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(BaseType)).Concat(Interfaces).Concat(Members).Where(n => n != null);

        public string Identifier { get; set; }

        public TypeSyntax BaseType { get; set; }

        public virtual bool IsInterface { get; set; }

        public List<TypeSyntax> Interfaces { get; set; } = new();

        public List<string> InnerComments { get; set; } = new();

        public List<MemberDeclarationSyntax> Members { get; set; } = new();

        // the following members are kept for the unit testing purposes only
        public List<ConstructorDeclarationSyntax> Constructors => Members.OfType<ConstructorDeclarationSyntax>().ToList();

        public List<MethodDeclarationSyntax> Methods => Members.OfExactType<MethodDeclarationSyntax>().ToList();

        public List<FieldDeclarationSyntax> Fields => Members.OfType<FieldDeclarationSyntax>().ToList();

        public List<PropertyDeclarationSyntax> Properties => Members.OfType<PropertyDeclarationSyntax>().ToList();

        public List<EnumDeclarationSyntax> Enums => Members.OfType<EnumDeclarationSyntax>().ToList();

        public List<ClassDeclarationSyntax> InnerClasses => Members.OfType<ClassDeclarationSyntax>().ToList();

        public List<ClassInitializerSyntax> Initializers => Members.OfType<ClassInitializerSyntax>().ToList();
    }
}