namespace wave.langserver
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Newtonsoft.Json.Linq;
    using Lsp = Microsoft.VisualStudio.LanguageServer.Protocol;
    public class WaveException : Exception
    {
        public WaveException(string msg) : base(msg)
        {
            
        }
        public WaveException(string msg, Exception inner) : base(msg, inner)
        {
            
        }
    }
    public static class QsCompilerError
    {
        public static void Raise(string message, Exception inner = null)
        {
            if (inner is null)
                throw new WaveException(message);
            throw new WaveException(message, inner);
        }
        public static void Verify(bool condition, string message)
        {
            if (!condition)
                Raise(message);
        }
        public static T RaiseOnFailure<T>(Func<T> action, string msg)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception(msg);
            }
        }
        public static void RaiseOnFailure(Action action, string msg)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception(msg);
            }
        }
    }

    public static class Utils
    {
        
        /// <summary>
        /// Applies the action <paramref name="f"/> to the value <paramref name="x"/>.
        /// </summary>
        internal static void Apply<T>(this T x, Action<T> f) => f(x);

        /// <summary>
        /// Applies the function <paramref name="f"/> to the value <paramref name="x"/> and returns the result.
        /// </summary>
        internal static TOut Apply<TIn, TOut>(this TIn x, Func<TIn, TOut> f) => f(x);

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for reference types.</remarks>
        internal static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            where TResult : class =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());

        /// <summary>
        /// Projects each element of a sequence into a new form and discards null values.
        /// </summary>
        /// <remarks>Overload for value types.</remarks>
        internal static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct =>
            source.SelectMany(item =>
                selector(item)?.Apply(result => new[] { result })
                ?? Enumerable.Empty<TResult>());
        // language server tools -
        // wrapping these into a try .. catch .. to make sure errors don't go unnoticed as they otherwise would

        public static T TryJTokenAs<T>(JToken arg)
            where T : class =>
            QsCompilerError.RaiseOnFailure(() => arg.ToObject<T>(), $"failed cast jtoken to {typeof(T).Name}");

        
        
        private static ShowMessageParams? AsMessageParams(string text, MessageType severity) =>
            text == null ? null : new ShowMessageParams { Message = text, MessageType = severity };

        /// <summary>
        /// Shows the given text in the editor.
        /// </summary>
        internal static void ShowInWindow(this WaveLanguageServer server, string text, MessageType severity)
        {
            var message = AsMessageParams(text, severity);
            //QsCompilerError.Verify(server != null && message != null, "cannot show message - given server or text was null");
            _ = server.NotifyClientAsync(Methods.WindowShowMessageName, message);
        }

        /// <summary>
        /// Shows a dialog window with options (actions) to the user, and returns the selected option (action).
        /// </summary>
        internal static async Task<MessageActionItem> ShowDialogInWindowAsync(this WaveLanguageServer server, string text, MessageType severity, MessageActionItem[] actionItems)
        {
            var message =
                new ShowMessageRequestParams()
                {
                    Message = text,
                    MessageType = severity,
                    Actions = actionItems
                };
            return await server.InvokeAsync<MessageActionItem>(Methods.WindowShowMessageRequestName, message);
        }

        /// <summary>
        /// Logs the given text in the editor.
        /// </summary>
        internal static void LogToWindow(this WaveLanguageServer server, string text, MessageType severity)
        {
            var message = AsMessageParams(text, severity);
            //QsCompilerError.Verify(server != null && message != null, "cannot log message - given server or text was null");
            _ = server.NotifyClientAsync(Methods.WindowLogMessageName, message);
        }

        // tools related to project loading and file watching

        /// <summary>
        /// Attempts to apply the given mapper to each element in the given sequence.
        /// Returns a new sequence consisting of all mapped elements for which the mapping succeeded as out parameter,
        /// as well as a bool indicating whether the mapping succeeded for all elements.
        /// The returned out parameter is non-null even if the mapping failed on some elements.
        /// </summary>
        internal static bool TryEnumerate<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> mapper,
            out ImmutableArray<TResult> mapped)
        {
            var succeeded = true;
            var enumerator = source.GetEnumerator();

            T Try<T>(Func<T> getRes, T fallback)
            {
                try
                {
                    return getRes();
                }
                catch
                {
                    succeeded = false;
                    return fallback;
                }
            }

            bool TryMoveNext() => Try(enumerator.MoveNext, false);
            (bool, TResult) ApplyToCurrent() => Try(() => (true, mapper(enumerator.Current)), (false, default!));

            var values = ImmutableArray.CreateBuilder<TResult>();
            while (TryMoveNext())
            {
                var evaluated = ApplyToCurrent();
                if (evaluated.Item1)
                {
                    values.Add(evaluated.Item2);
                }
            }

            mapped = values.ToImmutable();
            return succeeded;
        }

        /// <summary>
        /// Attempts to enumerate the given sequence.
        /// Returns a new sequence consisting of all elements which could be accessed,
        /// as well as a bool indicating whether the enumeration succeeded for all elements.
        /// The returned out parameter is non-null even if access failed on some elements.
        /// </summary>
        internal static bool TryEnumerate<TSource>(this IEnumerable<TSource> source, out ImmutableArray<TSource> enumerated) =>
            source.TryEnumerate(element => element, out enumerated);

        /// <summary>
        /// The given log function is applied to all errors and warning
        /// raised by the ms build routine an instance of this class is given to.
        /// </summary>
        internal class MSBuildLogger : Logger
        {
            private readonly Action<string, MessageType> logToWindow;

            internal MSBuildLogger(Action<string, MessageType> logToWindow) =>
                this.logToWindow = logToWindow;

            public override void Initialize(IEventSource eventSource)
            {
                eventSource.ErrorRaised += (sender, args) =>
                    this.logToWindow?.Invoke(
                        $"MSBuild error in {args.File}({args.LineNumber},{args.ColumnNumber}): {args.Message}",
                        MessageType.Error);

                eventSource.WarningRaised += (sender, args) =>
                    this.logToWindow?.Invoke(
                        $"MSBuild warning in {args.File}({args.LineNumber},{args.ColumnNumber}): {args.Message}",
                        MessageType.Warning);
            }
        }
        /// <summary>
        /// unicode for '\n'
        /// </summary>
        internal const string LF = "\u000A";

        /// <summary>
        /// unicode for '\r'
        /// </summary>
        internal const string CR = "\u000D"; // officially only giving a line break in combination with subsequent \n, but also causes a line break on its own...

        /// <summary>
        /// unicode for a line separator char
        /// </summary>
        internal const string LS = "\u2028";

        /// <summary>
        /// unicode for a paragraph separator char
        /// </summary>
        internal const string PS = "\u2029";

        /// <summary>
        /// unicode for a next line char
        /// </summary>
        internal const string NEL = "\u0085";

        /// <summary>
        /// contains the regex string that matches any char that is not a linebreak
        /// </summary>
        private static readonly string NonBreakingChar = $"[^{LF}{CR}{LS}{PS}{NEL}]";

        /// <summary>
        /// contains the regex string that matches a line break recognized by VisualStudio
        /// </summary>
        private static readonly string LineBreak = $"{CR}{LF}|{LF}|{CR}|{LS}|{PS}|{NEL}";

        // utils related to tracking the text content of files

        /// <summary>
        /// matches everything that could could be used as a symbol
        /// </summary>
        internal static readonly Regex ValidAsSymbol = new Regex(@"^[\p{L}_]([\p{L}\p{Nd}_]*)$");

        /// <summary>
        /// matches qualified symbols before the starting position (right-to-left), including incomplete qualified
        /// symbols that end with a dot
        /// </summary>
        internal static readonly Regex QualifiedSymbolRTL =
            new Regex(@"([\p{L}_][\p{L}\p{Nd}_]*\.?)+", RegexOptions.RightToLeft);

        /// <summary>
        /// matches a line and its line ending, and a *non-empty* line without line ending at the end
        /// </summary>
        private static readonly Regex EditorLine = new Regex($"({NonBreakingChar}*({LineBreak}))|({NonBreakingChar}+$)");

        /// <summary>
        /// matches a CR, LF, or CRLF occurence at the end of a string
        /// </summary>
        public static readonly Regex EndOfLine = new Regex($"({LineBreak})$"); // NOTE: *needs* to fail, if no line breaking character exists (scope tracking depends on it)

        /// <summary>
        /// Splits the given text into multiple lines, with the line ending of each line included in the line.
        /// </summary>
        public static string[] SplitLines(string text)
        {
            var matches = EditorLine.Matches(text);
            var lines = new string[matches.Count];
            var found = matches.GetEnumerator();
            for (var i = 0; found.MoveNext(); i += 1)
            {
                lines[i] = ((Match)found.Current).Value;
            }
            return lines;
        }

        /// <summary>
        /// to be used as "counter-piece" to SplitLines
        /// </summary>
        [return: NotNullIfNotNull("content")]
        public static string? JoinLines(string[] content) =>
            content == null ? null : string.Join("", content); // *DO NOT MODIFY* how lines are joined - the compiler functionality depends on it!

        /// <summary>
        /// Given a string, replaces the range [<paramref name="startChar"/>, <paramref name="endChar"/>) with <paramref name="insert"/>.
        /// Returns null if the given text is null.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startChar"/> and <paramref name="endChar"/> do not denote a valid range within <paramref name="lineText"/>.</exception>
        [return: NotNullIfNotNull("lineText")]
        internal static string? GetChangedText(string? lineText, int startChar, int endChar, string insert)
        {
            if (lineText == null)
            {
                return null;
            }
            if (startChar < 0 || startChar > lineText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startChar));
            }
            if (endChar < startChar || endChar > lineText.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(endChar));
            }
            return lineText.Remove(startChar, endChar - startChar).Insert(startChar, insert);
        }
    }

    internal static class DotNetSdkHelper
    {
        private static readonly Regex DotNet31Regex = new Regex(@"^3\.1\.\d+", RegexOptions.Multiline | RegexOptions.Compiled);

        public static bool? IsDotNet31Installed()
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
            });
            if (process?.WaitForExit(3000) != true || process.ExitCode != 0)
            {
                return null;
            }

            var sdks = process.StandardOutput.ReadToEnd();
            return DotNet31Regex.IsMatch(sdks);
        }
    }
}