namespace wave.etc
{
    using System;
    using insomnia.syntax;
    using Sprache;

    internal static class ExpressionExtension
    {
        public static bool CanOptimization(this ExpressionSyntax exp)
        {
            if (exp.HasOptimized)
                return false;
            if (exp is ArgumentExpression arg)
                return true;
            if (exp is StringLiteralExpressionSyntax)
                return true;

            try
            {
                ForceOptimization(exp);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static ExpressionSyntax ForceOptimization(this ExpressionSyntax exp)
        {
            if (exp is ArgumentExpression arg)
                return ForceOptimization(arg.Value);
            if (exp is StringLiteralExpressionSyntax)
                return exp.AsOptimized();

            var result = new Expressive.Expression(exp.ExpressionString).Evaluate();

            if (result is float f)
                return new SingleLiteralExpressionSyntax(f).AsOptimized();
            return new WaveSyntax().LiteralExpression.End().Parse($"{result}").AsOptimized();
        }
    }
}