namespace wave.syntax
{
    using System.Linq;
    using Sprache;
    using stl;

    public partial class WaveSyntax
    {
        // examples: {}, { /* inner comments */ }, { int a = 0; return; } // trailing comments
        protected internal virtual Parser<BlockSyntax> Block =>
            from comments in CommentParser.AnyComment.Token().Many()
            from openBrace in Parse.Char('{').Token()
            from statements in Statement.Many()
            from closeBrace in Parse.Char('}').Commented(this)
            select new BlockSyntax
            {
                LeadingComments = comments.ToList(),
                Statements = statements.ToList(),
                InnerComments = closeBrace.LeadingComments.ToList(),
                TrailingComments = closeBrace.TrailingComments.ToList(),
            };
        // creates dummy generic parser for any expressions with matching braces
        protected internal virtual Parser<string> GenericExpressionInBraces(char open = '(', char close = ')') =>
            from openBrace in Parse.Char(open).Token()
            from expression in GenericExpressionCore().Optional()
            from closeBrace in Parse.Char(close).Token()
            select expression.GetOrElse(string.Empty).Trim();
        // creates dummy generic parser for expressions with matching braces allowing commas and semicolons by default
        protected internal virtual Parser<string> GenericExpressionCore(string forbidden = null, bool allowCurlyBraces = true)
        {
            var subExpressionParser = GenericNewExpression.Select(x => $" {x}")
                .Or(Parse.CharExcept("'/(){}[]" + forbidden).Except(GenericNewExpression).Many().Text().Token())
                .Or(Parse.Char('/').Then(_ => Parse.Chars('/', '*').Not()).Once().Return("/"))
                .Or(CommentParser.AnyComment.Return(string.Empty))
                .Or(StringLiteral)
                .Or(GenericExpressionInBraces('(', ')').Select(x => $"({x})"))
                .Or(GenericExpressionInBraces('[', ']').Select(x => $"[{x}]"));

            // optionally include support for curly braces
            if (allowCurlyBraces)
            {
                subExpressionParser = subExpressionParser
                    .Or(GenericExpressionInBraces('{', '}').Select(x => $"{{{x}}}"));
            }

            return
                from subExpressions in subExpressionParser.Many()
                let expr = string.Join(string.Empty, subExpressions)
                where !string.IsNullOrWhiteSpace(expr)
                select expr;
        }
        
        // dummy generic parser for expressions with matching braces
        protected internal virtual Parser<string> GenericExpression =>
            GenericExpressionCore(forbidden: ",;").Select(x => x.Trim());
        
        protected internal virtual Parser<string> KeywordExpressionStatement(string keyword) =>
            from key in Keyword(keyword).Token()
            from expr in GenericExpression.XOptional()
            from semicolon in Parse.Char(';')
            select expr.GetOrDefault();
        
        // examples: new Map<string, string>
        protected internal virtual Parser<string> GenericNewExpression =>
            from prev in WaveParserExtensions.PrevChar(c => !char.IsLetterOrDigit(c), "non-alphanumeric")
            from @new in Parse.IgnoreCase("new").Then(_ => Parse.LetterOrDigit.Not()).Token()
            from type in TypeReference.Token()
            select $"new {type.AsString()}";

    }
}