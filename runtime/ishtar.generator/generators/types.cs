namespace ishtar;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using vein.reflection;
using vein.runtime;
using vein.syntax;
using vein;
using InvocationExpression = vein.syntax.InvocationExpression;

public static class G_Types
{
    public static VeinTypeCode GetTypeCode(this ExpressionSyntax exp, GeneratorContext? gen = null)
    {
        if (exp is NullLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_NULL;
        if (exp is NumericLiteralExpressionSyntax num)
            return GetTypeCodeFromNumber(num);
        if (exp is BoolLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_BOOLEAN;
        if (exp is StringLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_STRING;
        if (exp is IdentifierExpression id && gen is not null)
        {
            try
            {
                var t = id.DetermineType(gen);

                if (t.IsGeneric)
                    return VeinTypeCode.TYPE_CLASS;
                return t.Class.TypeCode;
            }
            catch (Exception e)
            {
                gen.LogError($"Error determining type for identifier '{id}': {e.Message}", id);
                throw;
            }
        }

        return VeinTypeCode.TYPE_CLASS;
    }

    public static VeinClass GetType(this ExpressionSyntax exp, GeneratorContext gen)
    {
        if (exp is NumericLiteralExpressionSyntax num)
            return GetTypeCodeFromNumber(num).AsClass(gen.Module.Types);
        if (exp is BoolLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_BOOLEAN.AsClass(gen.Module.Types);
        if (exp is StringLiteralExpressionSyntax)
            return VeinTypeCode.TYPE_STRING.AsClass(gen.Module.Types);
        if (exp is NullLiteralExpressionSyntax)
            throw new NotSupportedException("NULL is not a type");
        if (exp is IdentifierExpression id && gen is not null)
        {
            try
            {
                var t = id.DetermineType(gen);

                if (t.IsGeneric)
                    throw new NotSupportedException($"Cannot summon type from generic declaration");
                return t.Class;
            }
            catch (Exception e)
            {
                gen.LogError($"Error determining type for identifier '{id}': {e.Message}", id);
                throw;
            }
        }

        throw new NotImplementedException();
    }


    public static (VeinClass, VeinClass) Fusce(this BinaryExpressionSyntax binary, GeneratorContext context)
    {
        var lt = binary.Left.DetermineType(context);
        var rt = binary.Right.DetermineType(context);

        return (lt, rt);
    }

    public static VeinClass ExplicitConversion(VeinClass t1, VeinClass t2)
    {
        throw new Exception($"ExplicitConversion: {t1?.FullName.NameWithNS} and {t2?.FullName.NameWithNS}");
    }

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

    public static VeinComplexType ResolveMemberType(this IEnumerable<ExpressionSyntax> chain, GeneratorContext context)
    {
        var t = default(VeinComplexType);
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

    public static IEnumerable<VeinComplexType> DetermineTypes(this ArgumentListExpression exps, GeneratorContext context)
        => exps.Arguments.DetermineTypes(context);

    public static IEnumerable<VeinComplexType> DetermineTypes(this List<TypeSyntax> exps, GeneratorContext context)
    {
        foreach (var type in exps)
            yield return context.ResolveType(type);
    }

    public static IEnumerable<VeinComplexType> DetermineTypes(this IEnumerable<ExpressionSyntax> exps, GeneratorContext context)
    {
        var list = exps.ToList();

        var d = list.Select(x => x.DetermineType(context))
            .Where(x => x is not null /* if failed determine skip analyze */).ToList();

        return d;
    }

    public static VeinComplexType DetermineType(this ExpressionSyntax exp, GeneratorContext context)
    {
        if (exp.CanOptimizationApply())
            return exp.ForceOptimization().DetermineType(context);
        if (exp is SizeOfFunctionExpression)
            return VeinTypeCode.TYPE_I4.AsClass()(Types.Storage);
        if (exp is LiteralExpressionSyntax literal)
            return literal.GetTypeCode().AsClass()(Types.Storage);
        if (exp is ArrayCreationExpression arr)
            return context.ResolveType(new TypeSyntax(NameSymbol.Array)
            {
                TypeParameters = [arr.Type.Typeword]
            });
        if (exp is IndexerAccessExpressionSyntax indexer)
            return indexer.ResolveReturnType(context);
        if (exp is AccessExpressionSyntax access)
            return access.ResolveType(context);
        if (exp is NewExpressionSyntax { IsArray: false } @new)
            return context.ResolveType(@new.TargetType.Typeword);
        if (exp is NewExpressionSyntax { IsArray: true } newArr)
            return context.ResolveType(new TypeSyntax(NameSymbol.Array)
            {
                TypeParameters = [newArr.TargetType.Typeword]
            });
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
        if (exp is NameOfFunctionExpression)
            return VeinTypeCode.TYPE_STRING.AsClass()(Types.Storage);
        if (exp is BinaryExpressionSyntax bin)
        {
            if (bin.OperatorType.IsLogic())
                return VeinTypeCode.TYPE_BOOLEAN.AsClass()(Types.Storage);
            var (lt, rt) = bin.Fusce(context);

            var overloaded = context.GetOverloadedOperator(lt, rt, bin.OperatorType);

            if (overloaded is not null)
                return overloaded.ReturnType;

            return lt == rt ? lt : ExplicitConversion(lt, rt);
        }

        if (exp is TypeAsFunctionExpression typeAs)
            return typeAs.Generics.Single().DetermineType(context);
        if (exp is TypeIsFunctionExpression)
            return VeinTypeCode.TYPE_BOOLEAN.AsClass()(Types.Storage);

        if (exp is UnaryExpressionSyntax unary)
        {
            if (unary is { Kind: SyntaxType.PostfixUnaryExpression } and { OperatorType: ExpressionType.Negate or ExpressionType.Not })
                return unary.Operand.DetermineType(context);

            if (unary.Kind == SyntaxType.PostfixUnaryExpression)
            {
                if (unary.Operand is not IdentifierExpression name)
                {
                    context.LogError($"PostfixUnary cannot determine expression. support only IdentifierExpression operand", exp);
                    throw new SkipStatementException();
                }
                var type = context.ResolveScopedIdentifierType(name);

                if (type.IsGeneric)
                {
                    context.LogError($"PostfixUnary expression cannot user with Generic Type. support only Function expression operand", exp);
                    throw new SkipStatementException();
                }

                if (type.Class.TypeCode != VeinTypeCode.TYPE_FUNCTION)
                {
                    context.LogError($"PostfixUnary expression cannot used with non function type, supported only function expression operand", exp);
                    throw new SkipStatementException();
                }

                return type;
            }

            if (unary.OperatorType.IsLogic())
                return VeinTypeCode.TYPE_BOOLEAN.AsClass()(Types.Storage);

            
            // todo
        }



        context.LogError($"[DetermineType] Cannot determine expression.", exp);
        throw new SkipStatementException();
    }

    public static VeinComplexType ResolveType(this AccessExpressionSyntax access, GeneratorContext context)
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
        context.LogError($"[ResolveType] Cannot determine expression.", lastToken);

        throw new SkipStatementException();
    }

    public static VeinComplexType ResolveReturnType(this InvocationExpression inv, GeneratorContext context)
        => context.ResolveMethod(context.CurrentMethod.Owner, inv)?.ReturnType;


    public static VeinComplexType ResolveReturnType(this IndexerAccessExpressionSyntax indexerAccess,
        GeneratorContext context)

    {
        var accessType = indexerAccess.ToChain().SkipLast(1).ResolveMemberType(context);
        if (accessType.IsGeneric)
        {
            context.LogError($"[ResolveReturnType][IndexerAccessExpressionSyntax] Cannot resolve indexer access type for generic {accessType.TypeArg}.", indexerAccess);
            throw new SkipStatementException();
        }

        if (indexerAccess is { Right: ArgumentListExpression accessArguments })
        {
            var argTypes = accessArguments.Arguments.DetermineTypes(context).ToList();

            var accessMethod = accessType.Class.FindMethod(VeinArray.ArrayAccessGetterMethodName, argTypes.ToList());

            return accessMethod.ReturnType;
        }

        context.LogError($"[ResolveReturnType][IndexerAccessExpressionSyntax] indexerAccess invalid asp type.", indexerAccess);
        throw new SkipStatementException();
    }

    public static VeinComplexType ResolveReturnType(this InvocationExpression member,
        GeneratorContext context, IEnumerable<ExpressionSyntax> chain)
    {
        var t = default(VeinComplexType);
        var prev_id = default(IdentifierExpression);
        var enumerator = chain.ToArray();
        for (var i = 0; i != enumerator.Length; i++)
        {
            if (enumerator[i] is LiteralExpressionSyntax literal)
            {
                t = literal.GetTypeCode().AsClass()(Types.Storage);
                continue;
            }

            if (enumerator[i] is ThisAccessExpression)
            {
                t = context.CurrentMethod.Owner;
                continue;
            }

            if (enumerator[i] is SelfAccessExpression)
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
