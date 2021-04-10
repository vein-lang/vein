namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using extensions;
    using Sprache;

    public abstract class BaseSyntax : IPositionAware<BaseSyntax>, IPassiveParseTransition
    {
        public abstract SyntaxType Kind { get; }
        
        public abstract IEnumerable<BaseSyntax> ChildNodes { get; }
        
        protected IEnumerable<BaseSyntax> GetNodes(params BaseSyntax[] nodes) =>
            nodes.EmptyIfNull().Where(n => n != null);
        
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

        public bool IsBrokenToken => (this as IPassiveParseTransition).Error != null;
        
        internal Transform Transform { get; set; }
        
        PassiveParseError IPassiveParseTransition.Error { get; set; }
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