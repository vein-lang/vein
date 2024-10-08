namespace ishtar;

using System;
using emit;
using vein.runtime;
using vein.syntax;
using System.Linq;
using vein;

public static class G_Emitters
{
    public static ILGenerator EmitExpression(this ILGenerator gen, ExpressionSyntax expr)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (!ctx.DisableOptimization)
        {
            if (!expr.HasOptimized && ctx.CanOptimizationApply(expr))
                return gen.EmitExpression(ctx.ForceOptimization(expr));
        }

        if (expr is EtherealFunctionExpression ethereal)
            return gen.EmitEtherealMacro(ethereal);

        if (expr is AccessExpressionSyntax access)
            return gen.EmitAccess(access); ;

        if (expr is ArrayCreationExpression arr)
            return gen.EmitArrayCreate(arr);

        if (expr is LiteralExpressionSyntax literal)
            return gen.EmitLiteral(literal);

        if (expr is NewExpressionSyntax @new)
        {
            gen.EmitCreateObject(@new);
            return gen;
        }

        if (expr is ArgumentExpression arg)
            return gen.EmitExpression(arg.Value);

        if (expr is IdentifierExpression @ref)
            return gen.EmitLoadIdentifierReference(@ref);

        if (expr is ThisAccessExpression)
            return gen.EmitThis();

        if (expr is InvocationExpression inv)
        {
            return gen.EmitCall(ctx.CurrentMethod.Owner, inv);
        }

        if (expr is UnaryExpressionSyntax unary)
            return gen.EmitUnary(unary);

        if (expr is BinaryExpressionSyntax bin)
        {
            gen.EmitBinaryExpression(bin);
            return gen;
        }

        throw new NotImplementedException();
    }


    public static ILGenerator EmitEtherealMacro(this ILGenerator gen, EtherealFunctionExpression fn)
    {
        if (fn is TypeAsFunctionExpression typeAs)
            return gen.EmitTypeAsMacro(typeAs);
        if (fn is NameOfFunctionExpression nameOf)
            return gen.EmitNameOf(nameOf);
        if (fn is TypeIsFunctionExpression typeIs)
            return gen.EmitTypeIs(typeIs);
        if (fn is TypeOfFunctionExpression typeOf)
            return gen.EmitTypeOf(typeOf);
        if (fn is SizeOfFunctionExpression sizeOf)
            return gen.EmitSizeOf(sizeOf);
        throw new NotImplementedException();
    }

    public static ILGenerator EmitSizeOf(this ILGenerator gen, SizeOfFunctionExpression sizeOf)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        var t = context.ResolveType(sizeOf.Generics.Single());

        if (t.IsGeneric)
        {
            context.LogError($"sizeof cannot use with generics [limitation is temporary, awaiting natural structs and fully generics implementation].", sizeOf);
            throw new SkipStatementException();
        }

        var naturalSize = t.Class.TypeCode.GetNativeSize();

        return naturalSize switch
        {
            0 => throw new InvalidOperationException(),
            1 => gen.Emit(OpCodes.LDC_I4_1),
            2 => gen.Emit(OpCodes.LDC_I4_2),
            3 => gen.Emit(OpCodes.LDC_I4_3),
            4 => gen.Emit(OpCodes.LDC_I4_4),
            5 => gen.Emit(OpCodes.LDC_I4_5),
            _ => gen.Emit(OpCodes.LDC_I4_S, naturalSize),
        };
    }

    public static ILGenerator EmitTypeOf(this ILGenerator gen, TypeOfFunctionExpression nameOf)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (nameOf.Expression.IsDefined)
        {
            if (nameOf.Expression.Get() is IdentifierExpression id)
                return gen.Emit(OpCodes.LD_TYPE, context.ResolveType(id));
            return gen.EmitExpression(nameOf.Expression.Get()).Emit(OpCodes.LD_TYPE_E);
        }
        if (nameOf.Generics.Count == 0)
        {
            context.LogError($"as type required type argument", nameOf);
            throw new SkipStatementException();
        }
        if (nameOf.Generics.Count != 1)
        {
            context.LogError($"as type required single type argument", nameOf);
            throw new SkipStatementException();
        }
        var type = context.ResolveType(nameOf.Generics.Single());
        return gen.Emit(OpCodes.LD_TYPE, type);
    }

    public static ILGenerator EmitNameOf(this ILGenerator gen, NameOfFunctionExpression nameOf)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        // TODO, validate exist variable\field\method\class\etc
        if (!nameOf.Expression.IsDefined)
        {
            context.LogError($"nameof required single type argument", nameOf);
            throw new SkipStatementException();
        }
        if (nameOf.Expression.Get() is AccessExpressionSyntax nameof_exp1)
            return gen.Emit(OpCodes.LDC_STR, nameof_exp1.Right.ToString());
        if (nameOf.Expression.Get() is IdentifierExpression nameof_exp2)
            return gen.Emit(OpCodes.LDC_STR, nameof_exp2.ToString());
        context.LogError($"Target expression is not valid named expression.", nameOf);
        throw new SkipStatementException();
    }

    public static ILGenerator EmitTypeIs(this ILGenerator gen, TypeIsFunctionExpression typeOf)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        if (!typeOf.Expression.IsDefined)
        {
            context.LogError($"type is required single type argument", typeOf);
            throw new SkipStatementException();
        }
        gen.EmitExpression(typeOf.Expression.Get()).Emit(OpCodes.LD_TYPE_E);
        var t = context.ResolveType(typeOf.Generics.Single());

        gen.Emit(t.IsGeneric ? OpCodes.LD_TYPE_G : OpCodes.LD_TYPE, t);

        gen.Emit(OpCodes.EQL_T);

        return gen;
    }

    public static ILGenerator EmitTypeAsMacro(this ILGenerator gen, TypeAsFunctionExpression typeAs)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        if (typeAs.Generics.Count == 0)
        {
            context.LogError($"as type required type argument", typeAs);
            throw new SkipStatementException();
        }
        if (typeAs.Generics.Count != 1)
        {
            context.LogError($"as type required single type argument", typeAs);
            throw new SkipStatementException();
        }
        var type = context.ResolveType(typeAs.Generics.Single());
        var fromType = typeAs.Expression.Get().DetermineType(context);

        if (!type.IsGeneric && !fromType.IsGeneric)
        {
            if (type.Class.FullName.Equals(fromType.Class.FullName))
            {
                context.LogWarning("useless construction, casting types of identical types", typeAs);
                return gen; // no needed execute cast when type is equal
            }
        }

        return gen.EmitExpression(typeAs.Expression.Get()).Emit(OpCodes.CAST_G, fromType, type);
    }

    public static ILGenerator EmitThis(this ILGenerator gen, bool rly = true)
    {
        if (rly)
            return gen.Emit(OpCodes.LDARG_0); // load this
        return gen;
    }


    public static void EmitGenericsLoad(this ILGenerator gen, VeinClass @for, TypeExpression declaration)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");


        if (@for.TypeArgs.Count != declaration.Typeword.TypeParameters.Count)
        {
            context.LogError($"Generics error", declaration);
            throw new SkipStatementException();
        }

        var resolvedTypes = declaration.Typeword.TypeParameters.DetermineTypes(context).ToList();

        // TODO check constraints

        foreach (var type in resolvedTypes)
            gen.Emit(type.IsGeneric ? OpCodes.LD_TYPE_G : OpCodes.LD_TYPE, type);
    }

    public static void EmitCreateObject(this ILGenerator gen, NewExpressionSyntax @new)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var complexType = context.ResolveType(@new.TargetType.Typeword);

        if (complexType.IsGeneric)
        {
            context.LogError($"Cannot create an instance of the generic class '{complexType}', currently is not support empty ctor constraint", @new.TargetType);
            throw new SkipStatementException();
        }

        var type = complexType.Class;

        if (type.IsStatic)
        {
            context.LogError($"Cannot create an instance of the static class '{type}'", @new.TargetType);
            throw new SkipStatementException();
        }

        if (@new.IsObject)
        {
            var args = ((ObjectCreationExpression)@new.CtorArgs).Args.Arguments.ToArray();

            if (type.IsGenericType)
                gen.EmitGenericsLoad(type, @new.TargetType);


            gen.Emit(OpCodes.NEWOBJ, type);
            foreach (var arg in args)
                gen.EmitExpression(arg);

            var ctor = type.FindMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, args.DetermineTypes(context).ToList());

            if (ctor is null)
            {
                context.LogError(
                    $"'{type}' does not contain a constructor that takes '{args.Length}' arguments.",
                    @new.TargetType);
                throw new SkipStatementException();
            }

            gen.Emit(OpCodes.CALL, ctor);
            return;
        }

        if (@new.IsArray)
        {
            var exp = (ArrayInitializerExpression)@new.CtorArgs;
            var init_method = gen.ConstructArrayTypeInitialization(@new.TargetType,
                    exp);

            if (init_method is not null) // when method for init array is successful created/resolved
            {
                gen.Emit(OpCodes.CALL, init_method);
                return;
            }

            var sizes = exp.Sizes;

            if (sizes.Length > 1)
                throw new NotSupportedException($"Currently array rank greater 1 not supported.");

            var size = sizes.Single();

            gen.Emit(OpCodes.LD_TYPE, type);
            gen.EmitExpression(size); // todo maybe need resolve cast problem
            gen.Emit(OpCodes.NEWARR);
            return;
        }

        throw new Exception("EmitCreateObject");
    }

    public static void EmitBlock(this ILGenerator gen, BlockSyntax block)
    {
        if (block is EmptyBlockSyntax)
            return;
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        using var scope = ctx.CurrentScope.EnterScope();
        foreach (var v in block.Statements)
            gen.EmitStatement(v);
    }

    public static void EmitDefault(this ILGenerator gen, VeinTypeCode code)
    {
        switch (code)
        {
            case VeinTypeCode.TYPE_OBJECT:
            case VeinTypeCode.TYPE_STRING:
                gen.Emit(OpCodes.LDNULL);
                break;
            case VeinTypeCode.TYPE_CHAR:
            case VeinTypeCode.TYPE_I2:
            case VeinTypeCode.TYPE_BOOLEAN:
                gen.Emit(OpCodes.LDC_I2_0);
                break;
            case VeinTypeCode.TYPE_I4:
                gen.Emit(OpCodes.LDC_I4_0);
                break;
            case VeinTypeCode.TYPE_I8:
                gen.Emit(OpCodes.LDC_I8_0);
                break;
            case VeinTypeCode.TYPE_R4:
                gen.Emit(OpCodes.LDC_F4, .0f);
                break;
            case VeinTypeCode.TYPE_R8:
                gen.Emit(OpCodes.LDC_F8, .0d);
                break;
            default:
                throw new NotSupportedException($"{code}");
        }
    }

    public static ILGenerator EmitReturn(this ILGenerator generator, ReturnStatementSyntax statement) => statement switch
    {
        not { Expression: { } } => generator.Emit(OpCodes.RET),
        { Expression: LiteralExpressionSyntax lit }
            => generator.EmitLiteral(lit).Emit(OpCodes.RET),
        _ => generator.EmitExpression(statement.Expression).Emit(OpCodes.RET),
    };
}
