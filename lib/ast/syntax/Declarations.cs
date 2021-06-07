namespace mana.syntax
{
    using System;
    using Sprache;
    using stl;

    public partial class ManaSyntax
    {
        private Parser<ExpressionSyntax> _assignExpression =>
            from op in Parse.Char('=').Token()
            from exp in QualifiedExpression
            select exp;
        // auto i: int = 3;
        [Obsolete]
        protected internal virtual Parser<VariableDeclarationSyntax> VariableDeclaration =>
            (from keyword in KeywordExpression("auto").Token()
             from declarator in IdentifierExpression.Token().Commented(this)
             from @as in Parse.Char(':').Token().Commented(this)
             from type in TypeExpression.Token().Positioned().Commented(this)
             from exp in _assignExpression.Token().Optional()
             from semicolon in Parse.Char(';').Token()
             select new VariableDeclarationSyntax
             {
                 Type = type.Value,
                 Variable = declarator.Value,
                 ExpressionValue = exp.GetOrDefault()
             }
             .WithLeadingComments(declarator.LeadingComments).WithTrailingComments(declarator.TrailingComments)
             .WithLeadingComments(@as.LeadingComments).WithTrailingComments(@as.TrailingComments)
             .WithLeadingComments(type.LeadingComments).WithTrailingComments(type.TrailingComments)
            ).Positioned();
    }
}