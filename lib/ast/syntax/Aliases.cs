namespace vein.syntax;

using Sprache;

public partial class VeinSyntax
{
    internal virtual Parser<AliasSyntax> AliasDeclaration =>
        from global in KeywordExpression("global").Token().Optional()
        from keyword in KeywordExpression("alias").Token()
        from aliasName in IdentifierExpression.Token()
        from s in Parse.String("<|").Token()
        from body in MethodParametersAndBody.Token().Select(x => new TypeOrMethod(null, x))
            .Or(TypeExpression.Token().Then(_ => Parse.Char(';').Token().Return(_)).Select(x => new TypeOrMethod(x, null)))
        select new AliasSyntax(global.IsDefined, aliasName, body);
}
