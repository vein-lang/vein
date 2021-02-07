namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Sprache;
    using stl;

    public abstract class BaseSyntax : IPositionAware<BaseSyntax>
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
            transform = new Transform(startPos, length);
            return this;
        }
        
        internal Transform transform { get; set; }
    }

    public record Transform(Position pos, int len);
}