namespace mana.syntax
{
    using System.Collections.Generic;
    using Sprache;

    public class MemberDeclarationSyntax : BaseSyntax, IPositionAware<MemberDeclarationSyntax>
    {
        public MemberDeclarationSyntax() { }

        public MemberDeclarationSyntax(MemberDeclarationSyntax other = null)
        {
            this.WithProperties(other);
        }
        public override SyntaxType Kind => SyntaxType.ClassMember;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public List<AnnotationSyntax> Annotations { get; set; } = new();
        public List<ModificatorSyntax> Modifiers { get; set; } = new();

        public virtual MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName) => this;
        public virtual MemberDeclarationSyntax WithName(IdentifierExpression name) => this;


        public new MemberDeclarationSyntax SetPos(Position startPos, int length)
        {
            base.SetPos(startPos, length);
            return this;
        }
    }
}
