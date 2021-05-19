namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;

    public class MemberAccessSyntax : ExpressionSyntax
    {
        public IdentifierExpression MemberName { get; set; }
        public IdentifierExpression[] MemberChain { get; set; }
        
        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;
        public override SyntaxType Kind => SyntaxType.MemberAccessExpression;
        
        public override string ExpressionString
        {
            get
            {
                if (MemberChain is null || MemberChain.Length == 0)
                    return $"{MemberName}";
                return $"{string.Join(".", MemberChain.Select(x => x.ExpressionString))}.{MemberName}";
            }
        }
    }
}