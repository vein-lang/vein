namespace wave.syntax
{
    using System.Collections.Generic;

    public class LiteralExpressionSyntax : ExpressionSyntax
    {
        public LiteralExpressionSyntax(string token = null, LiteralType type = LiteralType.Null)
        {
            Token = token;
            LiteralType = type;
        }

        public override SyntaxType Kind => SyntaxType.LiteralExpression;

        public override void Accept(WaveSyntaxVisitor visitor) => visitor.VisitLiteralExpression(this);

        public override IEnumerable<BaseSyntax> ChildNodes => NoChildren;

        public string Token { get; set; }

        public LiteralType LiteralType { get; set; }


        public string UnwrapToken() => Token[1..^1];
    }
}