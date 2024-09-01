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
            if (exp is LiteralExpressionSyntax)
                return true;
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
            if (exp is BinaryExpressionSyntax { Left: IdentifierExpression, Right: IdentifierExpression })
                return false;
            if (exp is BinaryExpressionSyntax { Left: IdentifierExpression })
                return false;
            if (exp is BinaryExpressionSyntax { Right: IdentifierExpression })
                return false;
            if (exp is BinaryExpressionSyntax { Right: AccessExpressionSyntax })
                return false;
            if (exp is BinaryExpressionSyntax { Left: AccessExpressionSyntax })
                return false;
            if (exp is UnaryExpressionSyntax { Operand: ThisAccessExpression })
                return false;
            if (exp is UnaryExpressionSyntax { Operand: IdentifierExpression })
                return false;
            if (exp is UnaryExpressionSyntax { Operand: AccessExpressionSyntax })
                return false;
            if (exp is BinaryExpressionSyntax { Left: BinaryExpressionSyntax e1, Right: BinaryExpressionSyntax c1 })
                return e1.CanOptimizationApply() && c1.CanOptimizationApply();
            if (exp is BinaryExpressionSyntax { Left: LiteralExpressionSyntax, Right: LiteralExpressionSyntax })
                return true;

            try
            {
                new Expressive.Expression(exp.ExpressionString).Evaluate();
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
            if (exp is LiteralExpressionSyntax)
                return exp.AsOptimized();
            var result = new Expressive.Expression(exp.ExpressionString).Evaluate();

            if (result is float f)
                return float.IsInfinity(f) ? exp.AsOptimized() : new SingleLiteralExpressionSyntax(f).AsOptimized();

            if (result is double d)
                return double.IsInfinity(d) ? exp.AsOptimized() : new DoubleLiteralExpressionSyntax(d).AsOptimized();

            return new VeinSyntax().LiteralExpression.Positioned().End().Parse($"{result}").AsOptimized();
        }

        public static T Eval<T>(this ExpressionSyntax exp)
            => (T)new Expressive.Expression(exp.ExpressionString).Evaluate();
    }
}
