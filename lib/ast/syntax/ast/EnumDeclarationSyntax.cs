namespace insomnia.syntax
{
    using System.Collections.Generic;
    using System.Linq;

    public class EnumMemberDeclarationSyntax : MemberDeclarationSyntax
    {
        public EnumMemberDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.EnumMember;

        public string Identifier { get; set; }
    }
    
    public class EnumDeclarationSyntax : MemberDeclarationSyntax
    {
        public EnumDeclarationSyntax(MemberDeclarationSyntax heading = null)
            : base(heading)
        {
        }

        public override SyntaxType Kind => SyntaxType.Enum;

        public override IEnumerable<BaseSyntax> ChildNodes =>
            base.ChildNodes.Concat(Members).Where(n => n != null);

        public string Identifier { get; set; }

        public List<EnumMemberDeclarationSyntax> Members { get; set; } = new();

        public List<string> InnerComments { get; set; } = new();
    }
}