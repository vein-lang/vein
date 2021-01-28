namespace wave.syntax
{
    using Sprache;
    using stl;

    public partial class WaveSyntax : ICommentParserProvider
    {
        /// <example>
        /// "foo bar"
        /// "the \n bar \" foo"
        /// </example>
        protected internal virtual Parser<string> StringLiteral =>
            from leading in Parse.WhiteSpace.Many()
            from openQuote in Parse.Char('\"')
            from fragments in Parse.Char('\\').Then(_ => Parse.AnyChar.Select(c => $"\\{c}"))
                .Or(Parse.CharExcept("\\\"").Many().Text()).Many()
            from closeQuote in Parse.Char('\"')
            from trailing in Parse.WhiteSpace.Many()
            select $"\"{string.Join(string.Empty, fragments)}\"";
        
        /// <example>
        /// "str"
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> StringLiteralExpression =>
            from token in StringLiteral
            select new LiteralExpressionSyntax(token, LiteralType.String);
        
        /// <example>
        /// 1.23
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> DecimalLiteralExpression =>
            from token in Parse.DecimalInvariant
            select new LiteralExpressionSyntax(token, LiteralType.Numeric);

        /// <example>
        /// true
        /// false
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> BooleanLiteralExpression =>
            from token in Keyword("false").Or(Keyword("true"))
            select new LiteralExpressionSyntax(token, LiteralType.Boolean);

        /// <example>
        /// null
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> NullLiteralExpression =>
            from token in Keyword("null")
            select new LiteralExpressionSyntax(token, LiteralType.Null);
        
        /// <example>
        /// 1, true, 'hello'
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> LiteralExpression =>
            from expr in DecimalLiteralExpression.XOr(
                StringLiteralExpression).XOr(
                NullLiteralExpression).XOr(
                BooleanLiteralExpression).Commented(this)
            select expr.Value
                .WithLeadingComments(expr.LeadingComments)
                .WithTrailingComments(expr.TrailingComments);
        
        /// <example>
        /// (1+2*4), 'hello', (true == false ? 1 : 2), null
        /// </example>
        protected internal virtual Parser<ExpressionSyntax> FactorExpression =>
            GenericExpressionInBraces().Select(expr => new ExpressionSyntax("(" + expr + ")"))
                .XOr(LiteralExpression);
    }
}