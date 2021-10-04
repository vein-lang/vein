namespace vein.lsp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public class ManaCompilerError : Exception
    {
        public ManaCompilerError(string msg, Exception inner) : base(msg, inner)
        { }
        public static T? RaiseOnFailure<T>(Func<T> actor, string message)
        {
            try
            {
                return actor();
            }
            catch (Exception e)
            {
                throw new ManaCompilerError(message, e);
            }
        }
        public static void RaiseOnFailure(Action actor, string message)
        {
            try
            {
                actor();
            }
            catch (Exception e)
            {
                throw new ManaCompilerError(message, e);
            }
        }

        public static void Verify(bool b, string msg)
        {
            if (!b)
                throw new ManaCompilerError(msg, null);
        }

        internal static void Raise(string v) => throw new ManaCompilerError(v, null);
    }

    public static class Utils
    {
        /* language server tools -
         * wrapping these into a try .. catch .. to make sure errors don't go unnoticed as they otherwise would
         */

        public static readonly JsonSerializer JsonSerializer = new()
        {
            ContractResolver = new ResourceOperationKindContractResolver(),
        };

        public static T? TryJTokenAs<T>(JToken arg)
            where T : class =>
            ManaCompilerError.RaiseOnFailure(() => arg.ToObject<T>(JsonSerializer), "could not cast given JToken");

        private static ShowMessageParams? AsMessageParams(string text, MessageType severity) =>
            text == null ? null : new ShowMessageParams { Message = text, MessageType = severity };

        /// <summary>
        /// Shows the given text in the editor.
        /// </summary>
        internal static void ShowInWindow(this VeinLanguageServer server, string text, MessageType severity)
        {
            var message = AsMessageParams(text, severity);
            ManaCompilerError.Verify(server != null && message != null, "cannot show message - given server or text was null");
            _ = server.NotifyClientAsync(Methods.WindowShowMessageName, message);
        }

        /// <summary>
        /// Shows a dialog window with options (actions) to the user, and returns the selected option (action).
        /// </summary>
        internal static async Task<MessageActionItem> ShowDialogInWindowAsync
            (this VeinLanguageServer server, string text, MessageType severity, MessageActionItem[] actionItems)
        {
            var message =
                new ShowMessageRequestParams()
                {
                    Message = text,
                    MessageType = severity,
                    Actions = actionItems,
                };
            return await server.InvokeAsync<MessageActionItem>(Methods.WindowShowMessageRequestName, message);
        }

        /// <summary>
        /// Logs the given text in the editor.
        /// </summary>
        internal static void LogToWindow(this VeinLanguageServer server, string text, MessageType severity)
        {
            var message = AsMessageParams(text, severity);
            ManaCompilerError.Verify(server != null && message != null, "cannot log message - given server or text was null");
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
            this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
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
    }

    internal sealed class ResourceOperationKindContractResolver : DefaultContractResolver
    {
        private readonly ResourceOperationKindConverter rokConverter = new();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(ResourceOperationKind[]))
            {
                property.Converter = this.rokConverter;
            }

            return property;
        }
    }
    internal class ResourceOperationKindConverter : JsonConverter<ResourceOperationKind[]>
    {
        public override ResourceOperationKind[] ReadJson(JsonReader reader, Type objectType, ResourceOperationKind[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException($"Expected array start, got {reader.TokenType}.");
            }

            var values = new List<ResourceOperationKind>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }

                values.Add(reader.Value switch
                {
                    "create" => ResourceOperationKind.Create,
                    "delete" => ResourceOperationKind.Delete,
                    "rename" => ResourceOperationKind.Delete,
                    var badValue => throw new JsonSerializationException($"Could not deserialize {badValue} as ResourceOperationKind."),
                });
            }

            return values.ToArray();
        }

        public override void WriteJson(JsonWriter writer, ResourceOperationKind[]? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var element in value ?? Array.Empty<ResourceOperationKind>())
            {
                writer.WriteValue(element switch
                {
                    ResourceOperationKind.Create => "create",
                    ResourceOperationKind.Delete => "delete",
                    ResourceOperationKind.Rename => "rename",
                    _ => throw new JsonSerializationException($"Could not serialize {value} as ResourceOperationKind."),
                });
            }

            writer.WriteEndArray();
        }
    }
}
