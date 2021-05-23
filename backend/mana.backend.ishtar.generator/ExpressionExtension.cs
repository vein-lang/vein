namespace ishtar
{
    using Sprache;
    using mana.syntax;

    internal static class ExpressionExtension
    {
        public static bool CanOptimizationApply(this ExpressionSyntax exp)
        {
            if (exp.HasOptimized)
                return false;
            if (exp is ArgumentExpression arg)
                return arg.Value.CanOptimizationApply();
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
            return new ManaSyntax().LiteralExpression.End().Parse($"{result}").AsOptimized();
        }
    }
}