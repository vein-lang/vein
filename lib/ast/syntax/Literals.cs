namespace mana.syntax
{
    using System;
    using System.Linq;
    using Sprache;
    using stl;
    using mana.extensions;

    public partial class ManaSyntax
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
        /// 0b0101010_010101
        /// 0b010101010010101
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> BinaryLiteralExpression =>
            from zero in Parse.Char('0')
            from control in Parse.Chars("Bb")
            from chain in Parse.Char('_').Many().Then(_ => Parse.Chars("01")).AtLeastOnce().Text()
            from suffix in IntegerTypeSuffix.Optional()
            select FromBinary(chain.Replace("_", ""), suffix.GetOrDefault());
        [Flags]
        public enum NumericSuffix
        {
            None = 0,
            Long = 1 << 1,
            Unsigned = 1 << 2,
            Float = 1 << 3,
            Decimal = 1 << 4,
            Half = 1 << 5,
            Double = 1 << 6
        }

        private NumericLiteralExpressionSyntax TryParse<T, Z>(string str, Func<string, Z> parser)
            where Z : IFormattable, IConvertible, IComparable<Z>, IEquatable<Z>, IComparable
            where T : NumericLiteralExpressionSyntax<Z>
        {
            if (string.IsNullOrEmpty(str))
                throw new ParseException($"str is null.");
            if (str.EndsWith("."))
                throw new ParseException($"bad float.");
            try
            {
                parser(str);
            }
            catch (Exception e) when (char.IsDigit(str.First()))
            {
                return ErrorNumberLiteral(str, e);
            }
            catch (Exception e)
            {
                throw new ParseException(e.Message);
            }

            var ctor = typeof(T).GetConstructor(new[] {typeof(Z)});

            if (ctor is null)
                return ErrorNumberLiteral(str, new Exception($"ctor not found for type '{typeof(T)}'"));

            return (T)ctor.Invoke(new object?[] { parser(str) }); ;
        }

        private NumericLiteralExpressionSyntax ErrorNumberLiteral(string value, Exception e)
        {
            var err = new UndefinedIntegerNumericLiteral(value) as IPassiveParseTransition;
            err.Error = new PassiveParseError($"'{value}' is not literal number. [{e?.Message}]", new[] { "literal number." });
            return (UndefinedIntegerNumericLiteral)err;
        }

        private NumericLiteralExpressionSyntax FromBinary(string number, NumericSuffix? s)
        {
            var suffix = s ?? NumericSuffix.None;
            if (suffix.HasFlag(NumericSuffix.Long) && suffix.HasFlag(NumericSuffix.Unsigned))
                return TryParse<UInt64LiteralExpressionSyntax, ulong>(number, x => Convert.ToUInt64(x, 2));
            if (suffix.HasFlag(NumericSuffix.Long))
                return TryParse<Int64LiteralExpressionSyntax, long>(number, x => Convert.ToInt64(x, 2));
            if (suffix.HasFlag(NumericSuffix.Unsigned))
                return TryParse<UInt32LiteralExpressionSyntax, uint>(number, x => Convert.ToUInt32(x, 2));

            return TryParse<Int32LiteralExpressionSyntax, int>(number, x => Convert.ToInt32(x, 2));
        }
        private NumericLiteralExpressionSyntax FromDefault(string number, NumericSuffix? s)
        {
            var suffix = s ?? NumericSuffix.None;
            if (suffix.HasFlag(NumericSuffix.Long) && suffix.HasFlag(NumericSuffix.Unsigned))
                return TryParse<UInt64LiteralExpressionSyntax, ulong>(number, Convert.ToUInt64);
            if (suffix.HasFlag(NumericSuffix.Long))
                return TryParse<Int64LiteralExpressionSyntax, long>(number, Convert.ToInt64);
            if (suffix.HasFlag(NumericSuffix.Unsigned))
                return TryParse<UInt32LiteralExpressionSyntax, uint>(number, Convert.ToUInt32);

            var res = TryParse<Int32LiteralExpressionSyntax, int>(number, Convert.ToInt32);

            if (res.IsBrokenToken)
                return TryParse<Int64LiteralExpressionSyntax, long>(number, Convert.ToInt64);
            return res;
        }
        private NumericLiteralExpressionSyntax FromFloat(string number, NumericSuffix? s)
        {
            var suffix = s ?? NumericSuffix.None;
            if (suffix.HasFlag(NumericSuffix.Double))
                return TryParse<DoubleLiteralExpressionSyntax, double>(number, Convert.ToDouble);
            if (suffix.HasFlag(NumericSuffix.Decimal))
                return TryParse<DecimalLiteralExpressionSyntax, decimal>(number, Convert.ToDecimal);
            if (suffix.HasFlag(NumericSuffix.Half))
                return TryParse<HalfLiteralExpressionSyntax, float>(number, Convert.ToSingle);

            return TryParse<SingleLiteralExpressionSyntax, float>(number, Convert.ToSingle);
        }
        // [lL]? [uU] | [uU]? [lL]
        private Parser<NumericSuffix> IntegerTypeSuffix =>
            (from l in Parse.Chars("lL").Optional()
             from u in Parse.Chars("uU")
             select l.IsDefined ? NumericSuffix.Long | NumericSuffix.Unsigned : NumericSuffix.Unsigned).Or(
                from u in Parse.Chars("uU").Optional()
                from l in Parse.Chars("lL")
                select u.IsDefined ? NumericSuffix.Unsigned | NumericSuffix.Long : NumericSuffix.Long);

        private Parser<NumericSuffix> FloatTypeSuffix =>
            Parse.Chars("FfDdMmHh").Select(char.ToLowerInvariant).Select(x => x switch
            {
                'f' => NumericSuffix.Float,
                'd' => NumericSuffix.Double,
                'm' => NumericSuffix.Decimal,
                'h' => NumericSuffix.Half,
                _ => NumericSuffix.None
            });
        // [eE] ('+' | '-')? [0-9] ('_'* [0-9])*;
        internal Parser<string> ExponentPart =>
            Parse.Chars("eE").Then(x =>
                Parse.Chars("+-").Optional().Then(polarity =>
                    Parse.Number.Then(n => Parse.Char('_').Many().Then(_ => Parse.Number).Many()
                        .Select(w => $"{x}{polarity.GetOrDefault()}{n}{w.Join().Replace("_", "")}"
                            .Replace($"{default(char)}", "")))));
        // [0-9] ('_'* [0-9])*
        private Parser<string> NumberChainBlock =>
            from number in Parse.Number
            from chain in Parse.Char('_').Many().Then(_ => Parse.Number).Many().Select(x => x.Join())
            select $"{number}{chain.Replace("_", "")}";

        /// <example>
        /// 1.23d
        /// </example>
        protected internal virtual Parser<NumericLiteralExpressionSyntax> FloatLiteralExpression =>
            (from f1block in NumberChainBlock
             from dot in Parse.Char('.')
             from f2block in NumberChainBlock.AtLeastOnce()
             from exp in ExponentPart.Optional()
             from suffix in FloatTypeSuffix.Optional()
             select FromFloat($"{f1block}.{f2block.Join()}{exp.GetOrElse("")}",
                 suffix.GetOrDefault())).Or(
                    from block in NumberChainBlock
                    from other in FloatTypeSuffix.Select(x => ("", x)).Or(ExponentPart.Then(x =>
                        FloatTypeSuffix.Optional().Select(z => (x, z.GetOrDefault()))))
                    select FromFloat($"{block}{other.Item1}", other.Item2)
                );

        /// <example>
        /// 1124
        /// 111_241
        /// </example>
        protected internal virtual Parser<LiteralExpressionSyntax> IntLiteralExpression =>
            from number in NumberChainBlock
            from suffix in IntegerTypeSuffix.Optional()
            select FromDefault(number, suffix.GetOrDefault());

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
                FloatLiteralExpression.Log("FloatLiteralExpression").Or(
                        IntLiteralExpression.Log("IntLiteralExpression")).XOr(
                    StringLiteralExpression.Log("StringLiteralExpression")).XOr(
                    BinaryLiteralExpression.Log("BinaryLiteralExpression")).XOr(
                    BooleanLiteralExpression.Log("BooleanLiteralExpression")).XOr(
                    NullLiteralExpression.Log("NullLiteralExpression"))
                    .Positioned().Commented(this)
            select expr.Value
                .WithLeadingComments(expr.LeadingComments)
                .WithTrailingComments(expr.TrailingComments);
    }
}
