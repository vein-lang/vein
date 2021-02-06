namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using stl;

    public abstract class BaseSyntax
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
        
        private static Regex WhitespaceRegex { get; } = new Regex(@"\s+", RegexOptions.Compiled);
    }
}