namespace mana.syntax
{
    using Sprache;

    public partial class ManaSyntax
    {
        internal virtual Parser<DirectiveType> DirectiveDeclarator(DirectiveType type) =>
            from start in Parse.Char('#')
            from keyword in Parse.String(type.ToString().ToLowerInvariant())
            select type;

        internal virtual Parser<DirectiveSyntax> UseSyntax =>
            (from start in DirectiveDeclarator(DirectiveType.Use)
             from str in StringLiteralExpression.Token()
             select new UseSyntax
             {
                 Value = str
             }).Token().Named("use directive").Positioned();
        internal virtual Parser<DirectiveSyntax> SpaceSyntax =>
            (from start in DirectiveDeclarator(DirectiveType.Space)
             from str in StringLiteralExpression.Token()
             select new SpaceSyntax
             {
                 Value = str
             }).Token().Named("space directive").Positioned();
    }
}
