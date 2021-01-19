using Sprache;

namespace wave.stl
{
    using System.Collections.Generic;


    public enum OperatorKind
    {
        Access
    }
    public class Syntax
    {
        internal static readonly Dictionary<string, OperatorKind> Operators = new()
        {
            ["."] = OperatorKind.Access,
        };

        /// <summary>
        /// Keyword token
        /// </summary>
        /// <param name="keyword">
        /// keyword string
        /// </param>
        /// <example>
        /// Keyword("var").Parse("var")
        /// </example>
        public virtual Parser<string> Keyword(string keyword) =>
            (from word in Parse.String(keyword).Text() select word)
            .Token().Named($"keyword {keyword} token");
        /// <summary>
        /// Float number token
        /// </summary>
        /// <example>
        /// FloatToken.Parse("12.45") -> "12.45"
        /// FloatToken.Parse("-12.45") -> "-12.45"
        /// </example>
        public virtual Parser<string> FloatToken => (
                from minus in Parse.Char('-').Optional()
                from @string in Parse.DecimalInvariant
                select $"{minus.GetOrElse('+')}{@string}")
            .Token().Named("string token");
        /// <summary>
        /// hex number token
        /// </summary>
        /// <example>
        /// HexToken.Parse("0xDA") -> DA
        /// </example>
        public virtual Parser<string> HexToken =>
            (from zero in Parse.Char('0')
                from x in Parse.Chars("x")
                from number in Parse.Chars("ABCDEF1234567890").Many().Text()
                select number)
            .Token()
            .Named("hex number");
        /// <summary>
        /// string wrapped in double quote chars
        /// </summary>
        /// <example>
        /// StringToken.Parse("\"str\"") -> "str"
        /// </example>
        public virtual Parser<string> StringToken => (
            from open in Parse.Char('"')
            from value in Parse.CharExcept('"').Many().Text()
            from close in Parse.Char('"')
            select value)
            .Token().Named("string token");


        public virtual Parser<string> CommentToken =>
            from first in Parse.String("/*")
            from comment in Parse.AnyChar.Until(Parse.String("*/")).Text()
            select comment;


        protected internal virtual Parser<T> Wrap<T, S>(Parser<T> el, Parser<S> wrapper) =>
            from _1 in wrapper
            from result in el
            from _2 in wrapper
            select result;
    }
}