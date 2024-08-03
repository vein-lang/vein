namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using runtime;
    using Sprache;
    using stl;

    public interface IAdvancedPositionAware<out T> : IPositionAware<T>
    {
        public T SetStart(ExpressionSyntax s)
        {
            if (s.Transform is null)
                throw new InvalidOperationException(
                    $"[IAdvancedPositionAware::SetStart] '{s.GetType().Name}' has incorrect transform position.");

            StartPoint = s.Transform.pos;
            return (T)this;
        }

        public T SetEnd(ExpressionSyntax s)
        {
            if (s.Transform is null)
                throw new InvalidOperationException(
                    $"[IAdvancedPositionAware::SetEnd] '{s.GetType().Name}' has incorrect transform position.");

            EndPoint = s.Transform.pos;
            return (T)this;
        }

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

        public static ClassDeclarationSyntax Create(MemberDeclarationSyntax heading, ClassDeclarationSyntax classBody)
        {
            if (classBody.IsInterface)
                return new InterfaceDeclarationSyntax(heading, classBody);
            return new ClassDeclarationSyntax(heading, classBody)
            {
                GenericTypes = classBody.GenericTypes,
                TypeParameterConstraints = classBody.TypeParameterConstraints
            };
        }

        public override SyntaxType Kind => SyntaxType.Class;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes
                .Concat(Inheritances)
                .Concat(Fields)
                .Concat(Properties)
                .Concat(Members)
                .Concat([Identifier])
                .Where(n => n is not null);

        public IdentifierExpression Identifier { get; set; }

        public NameSymbol ClassName => new(Identifier.ExpressionString);

        public virtual bool IsInterface { get; set; }
        public virtual bool IsStruct { get; set; }
        public bool IsGeneric => GenericTypes.Any();

        public bool IsForwardedType => Aspects.Any(x => $"{x.Name}".Equals("forwarded"));

        public List<TypeSyntax> Inheritances { get; set; } = new();

        public List<string> InnerComments { get; set; } = new();

        public List<MemberDeclarationSyntax> Members { get; set; } = new();

        public List<MethodDeclarationSyntax> Methods => Members.OfExactType<MethodDeclarationSyntax>().ToList();

        public List<FieldDeclarationSyntax> Fields => Members.OfType<FieldDeclarationSyntax>().ToList();

        public List<PropertyDeclarationSyntax> Properties => Members.OfType<PropertyDeclarationSyntax>().ToList();
        public List<TypeParameterConstraintSyntax> TypeParameterConstraints { get; set; } = new();
        public List<TypeExpression> GenericTypes { get; set; } = new();

        public new ClassDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public override string ToString() => $"Class '{Identifier}'";
    }
}
