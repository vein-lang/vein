namespace wave.stl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Sprache;

    public static class WaveParserExtensions
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

        public static T ParseWave<T>(this Parser<T> parser, string input)
        {
            var result = parser.TryParse(input);
            if (result.WasSuccessful)
            {
                return result.Value;
            }
            throw new WaveParseException(result.Message, 
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
        public static Parser<ICommented<T>> Commented<T>(this Parser<T> parser, ICommentParserProvider provider)
        {
            return parser.Commented(provider?.CommentParser);
        }

        /// <summary>
        /// Single character look-behind parser.
        /// </summary>
        /// <param name="predicate">Predicate to match.</param>
        /// <param name="description">Parser description.</param>
        /// <param name="failByDefault">Whether the parser should fail on zero position.</param>
        public static Parser<char> PrevChar(Predicate<char> predicate, string description, bool failByDefault = false)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            return i =>
            {
                if (i.Position > 0 && i.Source.Length > 0)
                {
                    var prev = i.Source[i.Position - 1];
                    if (predicate(prev))
                    {
                        return Result.Success(prev, i);
                    }

                    return Result.Failure<char>(i,
                        $"unexpected '{prev}'",
                        new[] { description });
                }

                if (failByDefault)
                {
                    return Result.Failure<char>(i,
                        "Unexpected start of input reached",
                        new[] { description });
                }

                return Result.Success((char)0, i);
            };
        }
    }
    
    
    public interface ICommentParserProvider
    {
        IComment CommentParser { get; }
    }
    
    public class WaveParseException : ParseException
    {
        public WaveParseException(string message, Position pos)
            : base(message, pos)
        {
        }
    }
}