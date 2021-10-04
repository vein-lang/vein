namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sprache;

    public class MemberDeclarationSyntax : BaseSyntax, IPositionAware<MemberDeclarationSyntax>
    {
        public MemberDeclarationSyntax() { }

        public MemberDeclarationSyntax(MemberDeclarationSyntax other = null)
            => this.WithProperties(other);
        public override SyntaxType Kind => SyntaxType.ClassMember;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            Annotations.SelectMany(x => x.ChildNodes)
                .Concat(Modifiers.SelectMany(x => x.ChildNodes))
                .Concat(new[] { this });

        public List<AnnotationSyntax> Annotations { get; set; } = new();
        public List<ModificatorSyntax> Modifiers { get; set; } = new();

        public virtual MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName) => this;
        public virtual MemberDeclarationSyntax WithName(IdentifierExpression name) => this;


        public new MemberDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }

        public Position StartPoint { get; set; }
        public Position EndPoint { get; set; }

        public MemberDeclarationSyntax SetStart(Position startPos)
        {
            StartPoint = startPos;
            return this;
        }

        public MemberDeclarationSyntax SetEnd(Position endPos)
        {
            EndPoint = endPos;
            return this;
        }

        public bool IsInside(Position t)
        {
            if (EndPoint is null)
                return false;
            if (StartPoint is null)
                return false;
            return t.Line >= StartPoint.Line && t.Line <= EndPoint.Line;
        }

        public T As<T>() where T : BaseSyntax
        {
            if (this is T t)
                return t;
            throw new InvalidCastException($"Cant cast '{this.GetType().Name}' to {typeof(T).Name}.");
        }
    }
}
