namespace wave.syntax
{
    using System;
    using System.Globalization;
    using extensions;
    using Sprache;
    using stl;

    public partial class WaveSyntax
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
            select new StringLiteralExpressionSyntax(token);
        
        /// <example>
        /// 1.23m
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> DecimalLiteralExpression =>
            from polarity in Parse.Chars('-', '+').Optional()
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('m', 'M').Except(Parse.Chars('f', 'F', 'd', 'D', 'l', 'L'))
            select new DecimalLiteralExpressionSyntax(decimal.Parse($"{polarity.GetOrElse('+')}{token}", CultureInfo.InvariantCulture));
        
        /// <example>
        /// 1.23f
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> FloatLiteralExpression =>
            from polarity in Parse.Chars('-', '+').Optional()
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('f', 'F').Except(Parse.Chars('m', 'M', 'd', 'D', 'l', 'L'))
            select new SingleLiteralExpressionSyntax(float.Parse($"{polarity.GetOrElse('+')}{token}", CultureInfo.InvariantCulture));
        
        /// <example>
        /// 1.23d
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> DoubleLiteralExpression =>
            from polarity in Parse.Chars('-', '+').Optional()
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('d', 'D').Except(Parse.Chars('m', 'M', 'f', 'F', 'l', 'L'))
            select new DoubleLiteralExpressionSyntax(double.Parse($"{polarity.GetOrElse('+')}{token}", CultureInfo.InvariantCulture));
        
        /// <example>
        /// 1124
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> IntLiteralExpression =>
            from polarity in Parse.Chars('-', '+').Optional()
            from token in Parse.Number
            //from suffix in Parse.Chars('l', 'L').Except(Parse.Chars('m', 'M', 'f', 'F')).Optional()
            select transformNumber($"{polarity.GetOrElse('+')}{token}", default);

        protected internal virtual Parser<LiteralExpressionSyntax> NumericLiteralExpression =>
            from expr in
                DecimalLiteralExpression.Or(
                DoubleLiteralExpression).Or(
                FloatLiteralExpression).Or(
                IntLiteralExpression)
            select expr;
        
        // TODO rework detection int type
        private LiteralExpressionSyntax transformNumber(string token, char suffix)
        {
            if (!long.TryParse(token, out _))
                throw new ParseException("not valid integer.");
            
            if (suffix is 'l' or 'L')
                return new Int64LiteralExpressionSyntax(long.Parse(token));
            if (token.Length <= 5)
                return new Int16LiteralExpressionSyntax(short.Parse(token));
            if (token.Length <= 10)
                return new Int32LiteralExpressionSyntax(int.Parse(token));
            if (token.Length <= 19)
                return new Int64LiteralExpressionSyntax(long.Parse(token));
            throw new ParseException($"too big number '{token}'"); // TODO custom exception
        }
        
        /// <example>
        /// true
        /// false
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> BooleanLiteralExpression =>
            from token in Keyword("false").Or(Keyword("true"))
            select new BoolLiteralExpressionSyntax(token);

        /// <example>
        /// null
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> NullLiteralExpression =>
            from token in Keyword("null")
            select new NullLiteralExpressionSyntax();
        
        /// <example>
        /// 1, true, 'hello'
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> LiteralExpression =>
            from expr in 
                NumericLiteralExpression.XOr(
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