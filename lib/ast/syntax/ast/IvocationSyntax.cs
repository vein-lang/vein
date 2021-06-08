namespace mana.syntax
{
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using stl;

    public class InvocationExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxType Kind => SyntaxType.InvocationExpression;

        public string FunctionName { get; set; }
        public string[] MemberChain { get; set; }

        public override IEnumerable<BaseSyntax> ChildNodes => GetNodes(Expression).Concat(Arguments.EmptyIfNull());

        public ExpressionSyntax Expression { get; set; }

        public List<ExpressionSyntax> Arguments { get; set; } = new();
    }
}
