namespace wave.syntax
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
        
        public override MemberDeclarationSyntax WithName(string name)
        {
            Field.Identifier = name;
            return this;
        }

        public TypeSyntax Type { get; set; }

        public FieldDeclaratorSyntax Field { get; set; }

        public ClassDeclarationSyntax OwnerClass { get; set; }
    }
    
    public class ClassInitializerSyntax : MemberDeclarationSyntax
    {
        public ClassInitializerSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.ClassInitializer;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Body));

        public BlockSyntax Body { get; set; }

        public bool IsStatic => Modifiers.EmptyIfNull().Any(m => m.ModificatorKind == ModificatorKind.Static);
    }
    public class InterfaceDeclarationSyntax : ClassDeclarationSyntax
    {
        public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public InterfaceDeclarationSyntax(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody)
            : base(heading, classBody)
        {
        }

        public override SyntaxType Kind => SyntaxType.Interface;

        public override bool IsInterface => true;
    }
    public class PropertyDeclarationSyntax : MemberDeclarationSyntax
    {
        public PropertyDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public PropertyDeclarationSyntax(IEnumerable<AccessorDeclarationSyntax> accessors, MemberDeclarationSyntax heading = null)
            : this(heading)
        {
            Accessors = accessors.ToList();
        }

        public override SyntaxType Kind => SyntaxType.Property;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(GetNodes(Type, Getter, Setter));

        public TypeSyntax Type { get; set; }

        public string Identifier { get; set; }

        public List<AccessorDeclarationSyntax> Accessors { get; set; } = new();

        public AccessorDeclarationSyntax Getter => Accessors.FirstOrDefault(a => a.IsGetter);

        public AccessorDeclarationSyntax Setter => Accessors.FirstOrDefault(a => a.IsSetter);

        public override MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName)
        {
            Type = typeAndName.Type;
            Identifier = typeAndName.Identifier ?? typeAndName.Type.Identifier;
            return this;
        }
    }
    public class AccessorDeclarationSyntax : MemberDeclarationSyntax
    {
        public AccessorDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Accessor;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Body);

        public bool IsGetter { get; set; }

        public bool IsSetter => !IsGetter;

        public BlockSyntax Body { get; set; }

        public bool IsEmpty => Body == null;
    }
    
    public class FieldDeclaratorSyntax : BaseSyntax
    {
        public override SyntaxType Kind => SyntaxType.FieldDeclarator;

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression);

        public string Identifier { get; set; }

        public ExpressionSyntax Expression { get; set; }
    }
}