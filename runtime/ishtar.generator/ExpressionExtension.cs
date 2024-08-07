namespace ishtar
{
    using System;
    using System.Diagnostics;
    using Sprache;
    using vein.syntax;

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
            if (exp is IdentifierExpression)
                return false;
            if (exp is ThisAccessExpression)
                return false;
            if (exp is ArrayCreationExpression)
                return false;
            if (exp is NullLiteralExpressionSyntax)
                return false;
            if (exp is EtherealFunctionExpression)
                return false;
            if (exp is TypeExpression)
                return false;
            if (exp is NewExpressionSyntax)
                return false;
            if (exp is AccessExpressionSyntax)
                return false;
            if (exp is InvocationExpression)
                return false;

            try
            {
                ForceOptimization(exp);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[CanOptimizationApply] [{exp.GetType().Name}] {e}");
                return false;
            }
        }


        public static ExpressionSyntax ForceOptimization(this ExpressionSyntax exp)
        {
            if (exp is NullLiteralExpressionSyntax)
                return exp; // stupid Expressive.Expression evaluate null as null...
            if (exp is ArgumentExpression arg)
                return ForceOptimization(arg.Value);
            if (exp is StringLiteralExpressionSyntax)
                return exp.AsOptimized();

            var result = new Expressive.Expression(exp.ExpressionString).Evaluate();

            if (result is float f)
                return new SingleLiteralExpressionSyntax(f).AsOptimized();
            return new VeinSyntax().LiteralExpression.Positioned().End().Parse($"{result}").AsOptimized();
        }

        public static T Eval<T>(this ExpressionSyntax exp)
            => (T)new Expressive.Expression(exp.ExpressionString).Evaluate();
    }
}
