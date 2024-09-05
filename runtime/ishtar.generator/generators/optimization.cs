namespace ishtar;

using System;
using System.Diagnostics;
using System.Linq;
using Sprache;
using vein.runtime;
using vein.syntax;

internal static class ExpressionExtension
{
    public static bool CanOptimizationApply(this GeneratorContext ctx, ExpressionSyntax exp)
    {
        if (exp.HasOptimized)
            return false;
        if (exp is TypeAsFunctionExpression { IsRuntimeRequired: false })
            return true;
        if (exp is ArgumentExpression arg)
            return ctx.CanOptimizationApply(arg.Value);
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
            return ctx.CanOptimizationApply(e1) && ctx.CanOptimizationApply(c1);
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


    public static ExpressionSyntax ForceOptimization(this GeneratorContext ctx, ExpressionSyntax exp)
    {
        if (exp is NullLiteralExpressionSyntax)
            return exp; // stupid Expressive.Expression evaluate null as null...
        if (exp is ArgumentExpression arg)
            return ctx.ForceOptimization(arg.Value);
        if (exp is StringLiteralExpressionSyntax)
            return exp.AsOptimized();
        if (exp is LiteralExpressionSyntax)
            return exp.AsOptimized();
        if (exp is TypeAsFunctionExpression { IsRuntimeRequired: false } typeAs)
            return ctx.OptimizeTypeAs(typeAs);
        var result = new Expressive.Expression(exp.ExpressionString).Evaluate();

        if (result is float f)
            return float.IsInfinity(f) ? exp.AsOptimized() : new SingleLiteralExpressionSyntax(f).AsOptimized();

        if (result is double d)
            return double.IsInfinity(d) ? exp.AsOptimized() : new DoubleLiteralExpressionSyntax(d).AsOptimized();

        return new VeinSyntax().LiteralExpression.Positioned().End().Parse($"{result}").AsOptimized();
    }

    private static ExpressionSyntax OptimizeTypeAs(this GeneratorContext ctx, TypeAsFunctionExpression expression)
    {
        if (!expression.Expression.IsDefined)
            throw new InvalidOperationException();

        if (expression.Expression.Get() is LiteralExpressionSyntax literal)
        {
            var targetType = expression.Generics.First();
            var type = ctx.ResolveType(targetType);
            if (type.IsGeneric)
                throw new NotSupportedException();
            var typeCode = type.Class.TypeCode;

            if (typeCode == VeinTypeCode.TYPE_U1)
                return new ByteLiteralExpressionSyntax(literal.Eval<byte>()).SetPos<ByteLiteralExpressionSyntax>(literal.Transform).AsOptimized();
            if (typeCode == VeinTypeCode.TYPE_I1)
                return new SByteLiteralExpressionSyntax(literal.Eval<sbyte>()).SetPos<SByteLiteralExpressionSyntax>(literal.Transform).AsOptimized();

            if (typeCode == VeinTypeCode.TYPE_I2)
                return new Int16LiteralExpressionSyntax(literal.Eval<short>()).SetPos<Int16LiteralExpressionSyntax>(literal.Transform).AsOptimized();
            if (typeCode == VeinTypeCode.TYPE_U2)
                return new UInt16LiteralExpressionSyntax(literal.Eval<ushort>()).SetPos<UInt16LiteralExpressionSyntax>(literal.Transform).AsOptimized();

            if (typeCode == VeinTypeCode.TYPE_I4)
                return new Int32LiteralExpressionSyntax(literal.Eval<int>()).SetPos<Int32LiteralExpressionSyntax>(literal.Transform).AsOptimized();
            if (typeCode == VeinTypeCode.TYPE_U4)
                return new UInt32LiteralExpressionSyntax(literal.Eval<uint>()).SetPos<UInt32LiteralExpressionSyntax>(literal.Transform).AsOptimized();

            if (typeCode == VeinTypeCode.TYPE_I8)
                return new Int64LiteralExpressionSyntax(literal.Eval<long>()).SetPos<Int64LiteralExpressionSyntax>(literal.Transform).AsOptimized();
            if (typeCode == VeinTypeCode.TYPE_U8)
                return new UInt64LiteralExpressionSyntax(literal.Eval<ulong>()).SetPos<UInt64LiteralExpressionSyntax>(literal.Transform).AsOptimized();

        }


        return expression.AsOptimized();
    }

    public static T Eval<T>(this ExpressionSyntax exp)
    {
        var obj = new Expressive.Expression(exp.ExpressionString).Evaluate();
        return (T)Convert.ChangeType(obj, typeof(T));
    }
}
