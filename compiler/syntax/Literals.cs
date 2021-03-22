namespace wave.syntax
{
    using System;
    using System.Globalization;
    using System.Linq;
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
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('m', 'M').Except(Parse.Chars('f', 'F', 'd', 'D', 'l', 'L'))
            from e in Exponent.Optional()
            select new DecimalLiteralExpressionSyntax(decimal.Parse($"{token}{e.GetOrElse("")}", 
                CultureInfo.InvariantCulture));

        protected internal virtual Parser<NumericLiteralExpressionSyntax> BinaryLiteralExpression =>
            from zero in Parse.Char('0')
            from control in Parse.Chars("Bb") // ('_'* [01])+ IntegerTypeSuffix?
            from chain in Parse.Char('_').Many().Then(_ => Parse.Chars("01")).AtLeastOnce().Text()
            from suffix in IntegerTypeSuffix.Optional()
            select FromBinary(chain, suffix.GetOrDefault());
        [Flags]
        public enum NumericSuffix
        {
            None = 0,
            Long = 1 << 1,
            Unsigned = 1 << 2
        }

        private NumericLiteralExpressionSyntax FromBinary(string number, NumericSuffix? s)
        {
            var suffix = s ?? NumericSuffix.None;
            if (suffix.HasFlag(NumericSuffix.Long) && suffix.HasFlag(NumericSuffix.Unsigned))
                return new UInt64LiteralExpressionSyntax(Convert.ToUInt64(number, 2));
            if (suffix.HasFlag(NumericSuffix.Long))
                return new Int64LiteralExpressionSyntax(Convert.ToInt64(number, 2));
            if (suffix.HasFlag(NumericSuffix.Unsigned))
                return new UInt32LiteralExpressionSyntax(Convert.ToUInt32(number, 2));
            return new UndefinedIntegerNumericLiteral($"{Convert.ToInt64(number, 2)}");
        }
        // [lL]? [uU] | [uU]? [lL]
        private Parser<NumericSuffix> IntegerTypeSuffix =>
            (from l in Parse.Chars("lL").Optional()
                from u in Parse.Chars("uU")
                select l.IsDefined ? NumericSuffix.Long | NumericSuffix.Unsigned : NumericSuffix.Unsigned).Or(
                from u in Parse.Chars("uU").Optional()
                from l in Parse.Chars("lL")
                select u.IsDefined ? NumericSuffix.Unsigned | NumericSuffix.Long : NumericSuffix.Long);


        /// <example>
        /// 1.23f
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> FloatLiteralExpression =>
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('f', 'F').Except(Parse.Chars('m', 'M', 'd', 'D', 'l', 'L'))
            select new SingleLiteralExpressionSyntax(float.Parse($"{token}", CultureInfo.InvariantCulture));
        
        /// <example>
        /// 1.23d
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> DoubleLiteralExpression =>
            from token in Parse.DecimalInvariant
            from suffix in Parse.Chars('d', 'D').Except(Parse.Chars('m', 'M', 'f', 'F', 'l', 'L'))
            select new DoubleLiteralExpressionSyntax(double.Parse($"{token}", CultureInfo.InvariantCulture));
        
        /// <example>
        /// 1124
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> IntLiteralExpression =>
            from token in Parse.Number
            // so, why not automatic transform literal token (eg Int16LiteralExpression)
            // problem with UnaryExpression, negate symbol has parsed on top level combinators and into this combinator doesn't come
            // solution:
            //      define UndefinedIntegerNumericLiteral and into Unary combinator detection on construction level and replace for 
            //      proper variant.
            select new UndefinedIntegerNumericLiteral(token); 

        protected internal virtual Parser<LiteralExpressionSyntax> NumericLiteralExpression =>
            (from expr in
                    DecimalLiteralExpression.Or(
                        DoubleLiteralExpression).Or(
                        FloatLiteralExpression).Or(
                        IntLiteralExpression)
                select expr).Positioned();
        
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
                BooleanLiteralExpression).Positioned().Commented(this)
            select expr.Value
                .WithLeadingComments(expr.LeadingComments)
                .WithTrailingComments(expr.TrailingComments);
        
        protected internal virtual Parser<string> Exponent =>
            Parse.Chars("Ee").Then(e => Parse.Number.Select(n => "e+" + n).XOr(
                Parse.Chars("+-").Then(s => Parse.Number.Select(n => "e" + s + n))));
        
        
        protected internal virtual Parser<string> Binary =>
            Parse.IgnoreCase("0b").Then(x =>
                Parse.Chars("01").AtLeastOnce().Text()).Token();

        protected internal virtual Parser<string> Hexadecimal =>
            Parse.IgnoreCase("0x").Then(x =>
                Parse.Chars("0123456789ABCDEFabcdef").AtLeastOnce().Text()).Token();
    }
}