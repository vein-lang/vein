namespace wave.syntax
{
    using Sprache;

    public partial class WaveSyntax
    {
        internal virtual Parser<DirectiveType> DirectiveDeclarator(DirectiveType type) =>
            from start in Parse.Char('#')
            from keyword in Parse.String(type.ToString().ToLowerInvariant()) 
            select type;
        
        internal virtual Parser<UseSyntax> UseSyntax =>
            from start in DirectiveDeclarator(DirectiveType.Use)
            from str in StringLiteralExpression.Token()
            select new UseSyntax
            {
                Value = str
            };
        internal virtual Parser<SpaceSyntax> SpaceSyntax =>
            from start in DirectiveDeclarator(DirectiveType.Space)
            from str in StringLiteralExpression.Token()
            select new SpaceSyntax
            {
                Value = str
            };
    }
}