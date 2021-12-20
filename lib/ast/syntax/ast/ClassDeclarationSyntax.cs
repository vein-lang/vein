namespace vein.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;
    using stl;

    public interface IAdvancedPositionAware<out T> : IPositionAware<T>
    {
        public T SetStart(Position startPos)
        {
            StartPoint = startPos;
            return (T)this;
        }

        public T SetEnd(Position endPos)
        {
            EndPoint = endPos;
            return (T)this;
        }

        public bool IsInside(Position t)
        {
            if (EndPoint is null)
                return false;
            if (StartPoint is null)
                return false;
            return t.Line >= StartPoint.Line && t.Line <= EndPoint.Line;
        }

        public Position StartPoint { get; set; }
        public Position EndPoint { get; set; }
    }

    public class ClassDeclarationSyntax : MemberDeclarationSyntax, IAdvancedPositionAware<ClassDeclarationSyntax>
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
            base.ChildNodes
                .Concat(Inheritances)
                .Concat(Fields)
                .Concat(Properties)
                .Concat(Members)
                .Concat(new[] { Identifier })
                .Where(n => n != null);

        public IdentifierExpression Identifier { get; set; }

        public virtual bool IsInterface { get; set; }
        public virtual bool IsStruct { get; set; }

        public bool IsForwardedType => Aspects.Any(x => $"{x.Name}".Equals("forwarded"));

        public List<TypeSyntax> Inheritances { get; set; } = new();

        public List<string> InnerComments { get; set; } = new();

        public List<MemberDeclarationSyntax> Members { get; set; } = new();

        public List<MethodDeclarationSyntax> Methods => Members.OfExactType<MethodDeclarationSyntax>().ToList();

        public List<FieldDeclarationSyntax> Fields => Members.OfType<FieldDeclarationSyntax>().ToList();

        public List<PropertyDeclarationSyntax> Properties => Members.OfType<PropertyDeclarationSyntax>().ToList();


        public new ClassDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Class '{Identifier}'";
    }
}
