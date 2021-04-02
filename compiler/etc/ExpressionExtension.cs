namespace wave.etc
{
    using System;
    using insomnia.syntax;

    internal static class ExpressionExtension
    {
        public static bool CanOptimization(this ExpressionSyntax exp)
        {
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
                return exp;

            var result = new Expressive.Expression(exp.ExpressionString).Evaluate();

            if (result is float f)
                return new SingleLiteralExpressionSyntax(f);
            return new UndefinedIntegerNumericLiteral(result.ToString());
        }
    }
}