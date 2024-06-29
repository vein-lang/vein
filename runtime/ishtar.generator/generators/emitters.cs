namespace ishtar;

using System;
using emit;
using vein.runtime;
using vein.syntax;
using System.Linq;

public static class G_Emitters
{
    public static ILGenerator EmitExpression(this ILGenerator gen, ExpressionSyntax expr)
    {
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
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            return gen.EmitCall(ctx.CurrentMethod.Owner, inv);
        }

        if (expr is UnaryExpressionSyntax unary)
            return gen.EmitUnary(unary);

        if (expr is BinaryExpressionSyntax bin)
        {
            gen.EmitBinaryExpression(bin);
            return gen;
        }

        if (expr is NameOfExpressionSyntax @nameof)
        {
            // TODO, validate exist variable\field\method\class\etc
            if (@nameof.Expression is AccessExpressionSyntax nameof_exp1)
                return gen.Emit(OpCodes.LDC_STR, nameof_exp1.Right.ToString());
            if (@nameof.Expression is IdentifierExpression nameof_exp2)
                return gen.Emit(OpCodes.LDC_STR, nameof_exp2.ToString());
            var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
            ctx.LogError($"Target expression is not valid named expression.", expr);
            throw new SkipStatementException();
        }

        throw new NotImplementedException();
    }

    public static ILGenerator EmitThis(this ILGenerator gen, bool rly = true)
    {
        if (rly)
            return gen.Emit(OpCodes.LDARG_0); // load this
        return gen;
    }

    public static void EmitCreateObject(this ILGenerator gen, NewExpressionSyntax @new)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var type = context.ResolveType(@new.TargetType.Typeword);

        if (type.IsStatic)
        {
            context.LogError($"Cannot create an instance of the static class '{type}'", @new.TargetType);
            throw new SkipStatementException();
        }

        if (@new.IsObject)
        {
            var args = ((ObjectCreationExpression)@new.CtorArgs).Args.ToArray();
            gen.Emit(OpCodes.NEWOBJ, type);
            foreach (var arg in args)
                gen.EmitExpression(arg);
            var ctor = type.FindMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, args.DetermineTypes(context));

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
