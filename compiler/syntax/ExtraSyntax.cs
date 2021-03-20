namespace wave.syntax
{
    using Sprache;

    public partial class WaveSyntax
    {
        #region Wrappers

        private Parser<ExpressionSyntax> WrappedExpression(char open, char close) =>
            (from ob in Parse.Char(open).Token()
                from cb in Parse.Char(close).Token()
                select new ExpressionSyntax($"{ob}{cb}")).Token().Positioned();
        private Parser<ExpressionSyntax> WrappedExpression(string open, string close) =>
            (from ob in Parse.String(open).Token()
                from cb in Parse.String(close).Token()
                select new ExpressionSyntax($"{ob}{cb}")).Token().Positioned();
        private Parser<T> WrappedExpression<T>(string open, string close, Parser<T> parserUnit) =>
            (from ob in Parse.String(open).Token()
                from t in parserUnit.Token()
                from cb in Parse.String(close).Token()
                select t).Token();
        private Parser<T> WrappedExpression<T>(char open, char close, Parser<T> parserUnit) =>
            (from ob in Parse.Char(open).Token()
                from t in parserUnit.Token()
                from cb in Parse.Char(close).Token()
                select t).Token();

        #endregion
    }
}