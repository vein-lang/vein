namespace vein.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Sprache;

    public abstract class BaseSyntax : IPositionAware<BaseSyntax>, IPassiveParseTransition
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public abstract SyntaxType Kind { get; }

        [JsonIgnore]
        public abstract IEnumerable<BaseSyntax> ChildNodes { get; }

        protected IEnumerable<BaseSyntax> GetNodes(params BaseSyntax[] nodes) =>
            nodes.EmptyIfNull().Where(n => n != null);

        [JsonIgnore]
        protected IEnumerable<BaseSyntax> NoChildren => Enumerable.Empty<BaseSyntax>();

        public List<string> LeadingComments { get; set; } = new();
        public List<string> TrailingComments { get; set; } = new();

        public IEnumerable<BaseSyntax> DescendantNodes(Func<BaseSyntax, bool> descendIntoChildren = null) =>
            DescendantNodesAndSelf(descendIntoChildren).Skip(1);

        public IEnumerable<BaseSyntax> DescendantNodesAndSelf(Func<BaseSyntax, bool> descendIntoChildren = null)
        {
            yield return this;

            if (descendIntoChildren != null && !descendIntoChildren(this))
                yield break;
            foreach (var child in ChildNodes)
                foreach (var desc in child.DescendantNodesAndSelf(descendIntoChildren))
                    yield return desc;
        }

        public BaseSyntax SetPos(Position startPos, int length)
        {
            Transform = new Transform(startPos, length);
            return this;
        }

        public T SetPos<T>(Transform t) where T : BaseSyntax
        {
            Transform = new Transform(t.pos, t.len);
            return (T)this;
        }

        public bool IsBrokenToken => (this as IPassiveParseTransition).Error != null;

        internal Transform Transform { get; set; }

        PassiveParseError IPassiveParseTransition.Error { get; set; }

        public T MarkAsErrorWhen<T>(string error, bool mark) where T : BaseSyntax
        {
            if (!mark)
                return (T)this;
            return MarkAsError<T>(error);
        }
        public T MarkAsError<T>(string error) where T : BaseSyntax
        {
            (this as IPassiveParseTransition).Error = new PassiveParseError(error, new string[0]);
            return (T)this;
        }
    }

    public interface IPassiveParseTransition
    {
        PassiveParseError Error { get; set; }
        bool IsBrokenToken { get; }
    }

    public record Transform(Position pos, int len);

    public record PassiveParseError(string Message, IEnumerable<string> Expectations)
    {
        public string FormatExpectations()
            => Expectations.Select(x => $"'{x}'").Join(" or ");
    }
}
