namespace wave.syntax
{
    using System.Collections.Generic;

    public class MemberDeclarationSyntax : BaseSyntax
    {
        public MemberDeclarationSyntax(MemberDeclarationSyntax other = null)
        {
            this.WithProperties(other);
        }
        public override SyntaxType Kind => SyntaxType.ClassMember;
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public List<WaveAnnotationKind> Annotations { get; set; } = new();
        public List<string> Modifiers { get; set; } = new();

        public virtual MemberDeclarationSyntax WithTypeAndName(ParameterSyntax typeAndName) => this;
        public virtual MemberDeclarationSyntax WithName(string name) => this;
    }
}