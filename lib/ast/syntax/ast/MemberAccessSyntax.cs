namespace insomnia.syntax
{
    using System.Collections.Generic;

    public class MemberAccessSyntax : ExpressionSyntax
    {
        public string MemberName { get; set; }
        public string[] MemberChain { get; set; }
        
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        public override SyntaxType Kind => SyntaxType.MemberAccessExpression;
        
        public override string ExpressionString
        {
            get
            {
                if (MemberChain is null || MemberChain.Length == 0)
                    return $"{MemberName}";
                return $"{string.Join(".", MemberChain)}.{MemberName}";
            }
        }
    }
}