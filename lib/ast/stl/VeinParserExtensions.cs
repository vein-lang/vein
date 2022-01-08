namespace vein.stl
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Sprache;
    using syntax;

    public class Beacon<T> : IBeaconCommented<T>
    {
        public IEnumerable<string> LeadingComments => Comments.LeadingComments;
        public T Value => Comments.Value;
        public IEnumerable<string> TrailingComments => Comments.TrailingComments;

        public IBeaconCommented<T> SetPos(Position startPos, int length)
        {
            Transform = new Transform(startPos, length);
            return this;
        }

        public ICommented<T> Comments { get; }
        public Transform Transform { get; private set; }

        public Beacon(ICommented<T> c) => Comments = c;
    }

    public interface IBeaconCommented<T> : ICommented<T>, IPositionAware<IBeaconCommented<T>>
    {
        ICommented<T> Comments { get; }
        Transform Transform { get; }
    }

    public static class VeinParserExtensions
    {
        internal static Action<string> _log = s => {};
        static int _instance;

        public static Parser<T> Log<T>(this Parser<T> parser, string name)
        {
            var id = Interlocked.Increment(ref _instance);
            _log($"[{id}] Constructing instance of {name}");
            return i =>
            {
                _log($"[{id}] Invoking with input: {i}");
                var result = parser(i);
                _log($"[{id}] Result: {result}");
                return result;
            };
        }



        public static IEnumerable<T> GetOrEmpty<T>(this IOption<IEnumerable<T>> option)
            => option.GetOrElse(Array.Empty<T>());

        public static T ParseVein<T>(this Parser<T> parser, string input)
        {
            var result = parser.End().TryParse(input);
            if (result.WasSuccessful)
            {
                return result.Value;
            }
            throw new VeinParseException(result.Message,
                new Position(result.Remainder.Position, result.Remainder.Line, result.Remainder.Column));
        }

        /// <summary>
        /// Constructs a parser that consumes a whitespace and all comments
        /// parsed by the provider.Comment parser.
        /// </summary>
        /// <typeparam name="T">The result type of the given parser.</typeparam>
        /// <param name="parser">The parser to wrap.</param>
        /// <param name="provider">The provider for the Comment parser.</param>
        /// <returns>An extended Token() version of the given parser.</returns>
        public static Parser<T> Token<T>(this Parser<T> parser, ICommentParserProvider provider)
        {
            // if comment provider is not specified, act like normal Token()
            var trailingCommentParser =
                provider?.CommentParser?.AnyComment?.Token() ??
                Parse.WhiteSpace.Many().Text();

            // parse the value and as many trailing comments as possible
            return
                from value in parser.Commented(provider).Token()
                from comment in trailingCommentParser.Many()
                select value.Value;
        }

        /// <summary>
        /// Constructs a parser that consumes a whitespace and all comments
        /// parsed by the provider.Comment parser, but parses only one trailing
        /// comment that starts exactly on the last line of the parsed value.
        /// </summary>
        /// <typeparam name="T">The result type of the given parser.</typeparam>
        /// <param name="parser">The parser to wrap.</param>
        /// <param name="provider">The provider for the Comment parser.</param>
        /// <returns>An extended Token() version of the given parser.</returns>
        public static Parser<IBeaconCommented<T>> Commented<T>(this Parser<T> parser, ICommentParserProvider provider) =>
            parser.Commented(provider?.CommentParser)
                .Select(x => (IBeaconCommented<T>)new Beacon<T>(x)).Positioned();
        public static Parser<IdentifierExpression> Keyword(this string text) =>
            Parse.IgnoreCase(text).Then(_ => Parse.LetterOrDigit.Or(Parse.Char('_')).Not())
                .Return(new IdentifierExpression(text)).Positioned();

        public static Parser<IdentifierExpression> Literal(this string text) =>
            Parse.String(text).Token().Return(new IdentifierExpression(text)).Positioned();
        public static Parser<ExpressionSyntax> Downlevel<T>(this Parser<T> p) where T : ExpressionSyntax
            => p.Select(x => x.Downlevel());

        public static Parser<T> Parenthesis<T>(this Parser<T> parser) =>
            parser.Contained(VeinSyntax.OPENING_PARENTHESIS, VeinSyntax.CLOSING_PARENTHESIS);

        public static Parser<D> Then<T, D>(this Parser<T> parser, Parser<D> then) =>
            parser.Then(x => then);

        public static ExchangeWrapper<T> Exchange<T>(this Parser<T> p)
            => new(p);

        public struct ExchangeWrapper<T>
        {
            public ExchangeWrapper(Parser<T> _1) => this._ = _1;
            public Parser<T> _;

            public Parser<D> Return<D>() where D : class, new() => _.Return(new D());
        }
    }


    public interface ICommentParserProvider
    {
        IComment CommentParser { get; }
    }

    public class VeinParseException : ParseException
    {
        public VeinParseException(string message, Position pos)
            : base($"{message} at {pos}", pos) =>
            this.ErrorMessage = message;

        public string ErrorMessage { get; set; }
    }
}
