namespace insomnia.syntax
{
    using Sprache;
    using stl;

    public partial class WaveSyntax
    {
        // example: now = DateTime.Now()
        protected internal virtual Parser<VariableDeclaratorSyntax> VariableDeclarator =>
           (from identifier in Identifier
               from exp in Parse.Char('=').Token().Then(_ => QualifiedExpression).Positioned().Optional()
               select new VariableDeclaratorSyntax
               {
                   Identifier = identifier,
                   Expression = ExpressionSyntax.CreateOrDefault(exp),
               }).Positioned();
        // auto i: int = 3;
        protected internal virtual Parser<VariableDeclarationSyntax> VariableDeclaration =>
            (from keyword in Keyword("auto").Token()
                from declarator in VariableDeclarator.Commented(this)
                from @as in Parse.Char(':').Token().Commented(this)
                from type in TypeReference.Token().Positioned().Commented(this)
                from semicolon in Parse.Char(';')
                select new VariableDeclarationSyntax
                {
                    Type = type.Value,
                    Variables = declarator.Value,
                }
                    .WithLeadingComments(declarator.LeadingComments).WithTrailingComments(declarator.TrailingComments)
                    .WithLeadingComments(@as.LeadingComments).WithTrailingComments(@as.TrailingComments)
                    .WithLeadingComments(type.LeadingComments).WithTrailingComments(type.TrailingComments)
            ).Positioned();
    }
}