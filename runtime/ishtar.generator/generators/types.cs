namespace ishtar;

using System;
using System.Collections.Generic;
using System.Linq;
using vein.runtime;
using vein.syntax;

public static class G_Types
{
    public static VeinTypeCode GetTypeCode(this ExpressionSyntax exp)
    {
        if (exp is NumericLiteralExpressionSyntax num)
            return GetTypeCodeFromNumber(num);
        if (exp is BoolLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_BOOLEAN;
        if (exp is StringLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_STRING;
        if (exp is NullLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_NONE;
        return VeinTypeCode.TYPE_CLASS;
    }

    public static (VeinClass, VeinClass) Fusce(this BinaryExpressionSyntax binary, GeneratorContext context)
    {
        var lt = binary.Left.DetermineType(context);
        var rt = binary.Right.DetermineType(context);

        return (lt, rt);
    }

    public static VeinClass ExplicitConversion(VeinClass t1, VeinClass t2) =>
        throw new Exception($"ExplicitConversion: {t1?.FullName.NameWithNS} and {t2?.FullName.NameWithNS}");

    public static VeinTypeCode GetTypeCodeFromNumber(NumericLiteralExpressionSyntax number) =>
        number switch
        {
            ByteLiteralExpressionSyntax => VeinTypeCode.TYPE_U1,
            SByteLiteralExpressionSyntax => VeinTypeCode.TYPE_I1,
            Int16LiteralExpressionSyntax => VeinTypeCode.TYPE_I2,
            UInt16LiteralExpressionSyntax => VeinTypeCode.TYPE_U2,
            Int32LiteralExpressionSyntax => VeinTypeCode.TYPE_I4,
            UInt32LiteralExpressionSyntax => VeinTypeCode.TYPE_U4,
            Int64LiteralExpressionSyntax => VeinTypeCode.TYPE_I8,
            UInt64LiteralExpressionSyntax => VeinTypeCode.TYPE_U8,
            HalfLiteralExpressionSyntax => VeinTypeCode.TYPE_R2,
            SingleLiteralExpressionSyntax => VeinTypeCode.TYPE_R4,
            DoubleLiteralExpressionSyntax => VeinTypeCode.TYPE_R8,
            DecimalLiteralExpressionSyntax => VeinTypeCode.TYPE_R16,
            _ => throw new NotSupportedException($"{number} is not support number.")
        };

    public static VeinClass ResolveMemberType(this IEnumerable<ExpressionSyntax> chain, GeneratorContext context)
    {
        var t = default(VeinClass);
        var prev_id = default(IdentifierExpression);
        using var enumerator = chain.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var exp = enumerator.Current;
            if (exp is ThisAccessExpression)
                continue;
            if (exp is BaseAccessExpression)
                continue;
            if (exp is SelfAccessExpression)
                continue;
            if (exp is not IdentifierExpression id)
            {
                context.LogError($"Incorrect state of expression.", exp);
                throw new SkipStatementException();
            }

            t = t is null ?
                context.ResolveScopedIdentifierType(id) :
                context.ResolveField(t, prev_id, id)?.FieldType ??
                context.ResolveProperty(t, prev_id, id)?.PropType;
            prev_id = id;
        }

        return t;
    }

    public static IEnumerable<VeinClass> DetermineTypes(this IEnumerable<ExpressionSyntax> exps, GeneratorContext context)
        => exps.Select(x => x.DetermineType(context)).Where(x => x is not null /* if failed determine skip analyze */);

    public static VeinClass DetermineType(this ExpressionSyntax exp, GeneratorContext context)
    {
        if (exp.CanOptimizationApply())
            return exp.ForceOptimization().DetermineType(context);
        if (exp is LiteralExpressionSyntax literal)
            return literal.GetTypeCode().AsClass();
        if (exp is ArrayCreationExpression arr)
            return VeinTypeCode.TYPE_ARRAY.AsClass();
        if (exp is AccessExpressionSyntax access)
            return access.ResolveType(context);
        if (exp is NewExpressionSyntax { IsArray: false } @new)
            return context.ResolveType(@new.TargetType.Typeword);
        if (exp is NewExpressionSyntax { IsArray: true })
            return VeinTypeCode.TYPE_ARRAY.AsClass();
        if (exp is InvocationExpression inv)
            return inv.ResolveReturnType(context);
        if (exp is ArgumentExpression { Value: IdentifierExpression } arg1)
            return arg1.Value.DetermineType(context);
        if (exp is ArgumentExpression { Value: ThisAccessExpression })
            return context.CurrentMethod.Owner;
        if (exp is ThisAccessExpression)
            return context.CurrentMethod.Owner;
        if (exp is IdentifierExpression id)
            return context.ResolveScopedIdentifierType(id);
        if (exp is ArgumentExpression arg)
            return arg.Value.DetermineType(context);
        if (exp is TypeExpression t)
            return context.ResolveType(t.Typeword);
        if (exp is NameOfExpressionSyntax)
            return VeinTypeCode.TYPE_STRING.AsClass();
        if (exp is BinaryExpressionSyntax bin)
        {
            if (bin.OperatorType.IsLogic())
                return VeinTypeCode.TYPE_BOOLEAN.AsClass();
            var (lt, rt) = bin.Fusce(context);

            return lt == rt ? lt : ExplicitConversion(lt, rt);
        }
        context.LogError($"Cannot determine expression.", exp);
        throw new SkipStatementException();
    }

    public static VeinClass ResolveType(this AccessExpressionSyntax access, GeneratorContext context)
    {
        var chain = access.ToChain().ToArray();
        var lastToken = chain.Last();

        if (lastToken is InvocationExpression method)
            return method.ResolveReturnType(context, chain);
        if (lastToken is IndexerExpression)
        {
            context.LogError($"indexer is not support.", lastToken);
            return null;
        }
        if (lastToken is OperatorExpressionSyntax)
            return chain.SkipLast(1).ResolveMemberType(context);
        if (lastToken is IdentifierExpression)
            return chain.ResolveMemberType(context);
        context.LogError($"Cannot determine expression.", lastToken);

        throw new SkipStatementException();
    }

    public static VeinClass ResolveReturnType(this InvocationExpression inv, GeneratorContext context)
        => context.ResolveMethod(context.CurrentMethod.Owner, inv)?.ReturnType;

    public static VeinClass ResolveReturnType(this InvocationExpression member,
        GeneratorContext context, IEnumerable<ExpressionSyntax> chain)
    {
        var t = default(VeinClass);
        var prev_id = default(IdentifierExpression);
        var enumerator = chain.ToArray();
        for (var i = 0; i != enumerator.Length; i++)
        {
            if (enumerator[i] is LiteralExpressionSyntax literal)
            {
                t = literal.GetTypeCode().AsClass();
                continue;
            }

            if (enumerator[i] is ThisAccessExpression @this)
            {
                t = context.CurrentMethod.Owner;
                continue;
            }

            var exp = enumerator[i] as IdentifierExpression;

            if (exp is null && enumerator[i] is InvocationExpression inv)
                exp = inv.Name as IdentifierExpression;

            if (i + 1 == enumerator.Length)
                return context.ResolveMethod(t ?? context.CurrentMethod.Owner, prev_id, exp, member)
                    ?.ReturnType;
            t = t is null ?
                context.ResolveScopedIdentifierType(exp) :
                context.ResolveField(t, prev_id, exp)?.FieldType;
            prev_id = exp;
        }

        context.LogError($"Incorrect state of expression.", member);
        throw new SkipStatementException();
    }
}
