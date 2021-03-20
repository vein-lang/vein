namespace wave.syntax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        #region BinaryExp helpers

        private ExpressionSyntax FlatIfEmptyOrNull(ExpressionSyntax exp, IOption<ExpressionSyntax> option, string op)
        {
            if (option.IsDefined)
                return new BinaryExpressionSyntax(exp, option.Get(), op);
            return exp;
        }
        private ExpressionSyntax FlatIfEmptyOrNull(ExpressionSyntax exp, CoalescingExpressionSyntax coalescing)
        {
            if (coalescing is not { First: null, Second: null })
                return new BinaryExpressionSyntax(exp, coalescing, "?:");
            return exp;
        }
        private ExpressionSyntax FlatIfEmptyOrNull<T>(ExpressionSyntax exp, (string op, T exp)[] data) where T : ExpressionSyntax
        {
            if (data.Length == 0)
                return exp;
            if (data.Length == 1)
                return new BinaryExpressionSyntax(exp, data[0].exp, data[0].op);
            var e = exp;

            foreach (var (op, newExp) in data)
            {
                e = new BinaryExpressionSyntax(e, newExp, op);
            }

            return e;
        }
        private ExpressionSyntax FlatIfEmptyOrNull(ExpressionSyntax exp, IEnumerable<ExpressionSyntax> exps, string op)
        {
            if (exps.EmptyIfNull().Count() == 0)
                return exp;
            return new BinaryExpressionSyntax(exp, new MultipleBinaryChainExpressionSyntax(exps), op);
        }

        #endregion
        
    }
}