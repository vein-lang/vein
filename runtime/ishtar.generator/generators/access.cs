namespace ishtar;

using System;
using System.Linq;
using emit;
using Expressive;
using vein.reflection;
using vein.runtime;
using vein.syntax;

public static class G_Access
{
    [Flags]
    public enum AccessFlags
    {
        NONE = 0,
        VARIABLE = 1 << 1,
        ARGUMENT = 1 << 2,
        FIELD = 1 << 3,
        STATIC_FIELD = 1 << 4,
        CLASS = 1 << 5,
        PROPERTY = 1 << 6,
        STATIC_PROPERTY = 1 << 7
    }

    public static ILGenerator EmitStageField(this ILGenerator gen, VeinField field)
    {
        if (!field.IsStatic) return gen.Emit(OpCodes.STF, field);
        return gen.Emit(OpCodes.STSF, field);
    }

    public static ILGenerator EmitLoadArgument(this ILGenerator gen, int i) => i switch
    {
        0 => gen.Emit(OpCodes.LDARG_0),
        1 => gen.Emit(OpCodes.LDARG_1),
        2 => gen.Emit(OpCodes.LDARG_2),
        3 => gen.Emit(OpCodes.LDARG_3),
        4 => gen.Emit(OpCodes.LDARG_4),
        5 => gen.Emit(OpCodes.LDARG_5),
        _ => gen.Emit(OpCodes.LDARG_S, i)
    };
    public static ILGenerator EmitLoadIdentifierReference(this ILGenerator gen, IdentifierExpression id)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        // first order: search variable
        if (context.CurrentScope.HasVariable(id))
        {
            var (type, index) = context.CurrentScope.GetVariable(id);
            gen.WriteDebugMetadata($"/* access local, var: '{id}', index: '{index}', type: '{type}' */");
            return gen.EmitLoadLocal(index);
        }

        // second order: search argument
        var args = context.ResolveArgument(id);
        if (args is not null)
            return gen.EmitLoadArgument(args.Value.index);

        // third order: find field
        var field = context.ResolveField(id);
        if (field is not null)
        {
            if (field.IsStatic)
                return gen.Emit(OpCodes.LDSF, field);
            return gen.EmitThis().Emit(OpCodes.LDF, field);
        }

        var prop = context.ResolveProperty(id);
        if (prop is not null)
        {
            if (prop.IsStatic)
                return gen.Emit(OpCodes.CALL, prop.Getter);
            return gen.EmitThis().Emit(OpCodes.CALL, prop.Getter);
        }

        context.LogError($"The name '{id}' does not exist in the current context.", id);
        throw new SkipStatementException();
    }

    public static AccessFlags GetAccessFlags(this ILGenerator gen, IdentifierExpression id)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var flags = AccessFlags.NONE;

        var currentScope = context.CurrentScope;

        while (currentScope is not null)
        {
            if (currentScope.HasVariable(id))
            {
                flags |= AccessFlags.VARIABLE;
                break;
            }
            currentScope = currentScope.TopScope;
        }

        // second order: search argument
        var args = context.ResolveArgument(id);
        if (args is not null)
            flags |= AccessFlags.ARGUMENT;

        // third order: find field
        var field = context.ResolveField(id);
        if (field is { IsStatic: false })
            flags |= AccessFlags.FIELD;
        if (field is { IsStatic: true })
            flags |= AccessFlags.STATIC_FIELD;

        var prop = context.ResolveProperty(id);
        if (prop is { IsStatic: false })
            flags |= AccessFlags.PROPERTY;
        if (prop is { IsStatic: true })
            flags |= AccessFlags.STATIC_PROPERTY;

        // four order: find class
        if (context.HasDefinedType(id))
            flags |= AccessFlags.CLASS;

        return flags;
    }

    public static ILGenerator EmitAccess(this ILGenerator gen, AccessExpressionSyntax access)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (access is { Left: IdentifierExpression id, Right: InvocationExpression invoke })
        {
            var flags = gen.GetAccessFlags(id);

            // first order: variable
            if (flags.HasFlag(AccessFlags.VARIABLE))
            {
                var (@class, var_index) = ctx.CurrentScope.GetVariable(id);
                return gen.EmitLoadLocal(var_index).EmitCall(@class, invoke);
            }
            // second order: argument
            if (flags.HasFlag(AccessFlags.ARGUMENT))
            {
                var (arg, index) = ctx.GetCurrentArgument(id);
                return gen.EmitLoadArgument(index)
                    .EmitCall(arg.Type, invoke);
            }
            // three order: field
            if (flags.HasFlag(AccessFlags.FIELD))
            {
                var field = ctx.ResolveField(id);
                return gen.EmitThis().Emit(OpCodes.LDF, field)
                    .EmitCall(field.FieldType, invoke);
            }
            // four order: static field
            if (flags.HasFlag(AccessFlags.STATIC_FIELD))
            {
                var field = ctx.ResolveField(id);
                return gen.Emit(OpCodes.LDSF, field)
                    .EmitCall(field.FieldType, invoke);
            }
            // five order: static class
            if (flags.HasFlag(AccessFlags.CLASS))
            {
                var @class = ctx.ResolveType(id);
                return gen.EmitCall(@class, invoke);
            }

            return gen;
        }

        // literal access call
        if (access is { Left: LiteralExpressionSyntax literal, Right: InvocationExpression invoke1 })
        {
            var @class = literal.GetTypeCode(ctx).AsClass()(Types.Storage);
            return gen.EmitLiteral(literal).EmitCall(@class, invoke1);
        }

        if (access is { Left: ThisAccessExpression, Right: AccessExpressionSyntax otherAccess })
            return gen.EmitThis().EmitAccess(otherAccess);

        if (access is { Left: ThisAccessExpression @this, Right: IdentifierExpression id1 })
        {
            var flags = gen.GetAccessFlags(id1);

            // first order: variable
            if (flags.HasFlag(AccessFlags.VARIABLE))
            {
                var (_, var_index) = ctx.CurrentScope.GetVariable(id1);
                return gen.Emit(OpCodes.LDLOC_S, var_index);
            }
            // second order: argument
            if (flags.HasFlag(AccessFlags.ARGUMENT))
                ctx.LogError($"Keyword 'this' is not valid in a access to function argument.", @this);
            // three order: field
            if (flags.HasFlag(AccessFlags.FIELD))
            {
                var field = ctx.ResolveField(id1);
                return gen.EmitThis().Emit(OpCodes.LDF, field);
            }
            // four order: static field
            if (flags.HasFlag(AccessFlags.STATIC_FIELD))
                ctx.LogError($"Static member '{id1}' cannot be accessed with an instance reference.", id1);

            // five order: static prop
            if (flags.HasFlag(AccessFlags.PROPERTY))
            {
                var prop = ctx.ResolveProperty(id1);
                return gen.EmitThis().Emit(OpCodes.CALL, prop.Getter);
            }

            // six order: static prop
            if (flags.HasFlag(AccessFlags.STATIC_PROPERTY))
                ctx.LogError($"Static member '{id1}' cannot be accessed with an instance reference.", id1);

            // seven order: static class
            if (flags.HasFlag(AccessFlags.CLASS))
                ctx.LogError($"'{id1}' is a type, which is not valid in current context.", id1);

            return gen;
        }

        if (access is { Left: SelfAccessExpression self, Right: IdentifierExpression id2 })
        {
            var flags = gen.GetAccessFlags(id2);
            // first order: variable
            if (flags.HasFlag(AccessFlags.VARIABLE))
                ctx.LogError($"Keyword 'self' is not valid in a access to variable.", self);
            // second order: argument
            if (flags.HasFlag(AccessFlags.ARGUMENT))
                ctx.LogError($"Keyword 'self' is not valid in a access to function argument.", self);
            // three order: field
            if (flags.HasFlag(AccessFlags.FIELD))
                ctx.LogError($"Keyword 'self' is not valid in a access to non-static field.", self);
            // four order: static field
            if (flags.HasFlag(AccessFlags.STATIC_FIELD))
            {
                var field = ctx.ResolveField(id2);
                return gen.Emit(OpCodes.LDSF, field);
            }
            // five order: prop
            if (flags.HasFlag(AccessFlags.PROPERTY))
                ctx.LogError($"Keyword 'self' is not valid in a access to non-static propperty.", self);
            // six order: static prop
            if (flags.HasFlag(AccessFlags.STATIC_PROPERTY))
            {
                var prop = ctx.ResolveProperty(id2);
                return gen.Emit(OpCodes.CALL, prop.Getter);
            }
            // seven order: static class
            if (flags.HasFlag(AccessFlags.CLASS))
                ctx.LogError($"Keyword 'self' is not valid in a access to class.", self);

            return gen;
        }

        if (access is { Left: ThisAccessExpression, Right: InvocationExpression inv1 })
            return gen.EmitThis().EmitCall(ctx.CurrentMethod.Owner, inv1);
        if (access is { Left: SelfAccessExpression, Right: InvocationExpression inv2 })
            return gen.EmitCall(ctx.CurrentMethod.Owner, inv2);

        if (access is { Left: IdentifierExpression id6, Right: IdentifierExpression id7 })
        {
            var flags = gen.GetAccessFlags(id6);

            // first order: variable
            if (flags.HasFlag(AccessFlags.VARIABLE))
            {
                var (@class, var_index) = ctx.CurrentScope.GetVariable(id6);
                return gen.EmitLoadLocal(var_index).EmitLoadField(@class, id7);
            }
            // second order: argument
            if (flags.HasFlag(AccessFlags.ARGUMENT))
            {
                var (arg, index) = ctx.GetCurrentArgument(id6);
                return gen.EmitLoadArgument(index)
                    .EmitLoadField(arg.Type, id7);
            }
            // three order: field
            if (flags.HasFlag(AccessFlags.FIELD))
                return gen.EmitThis().EmitLoadField(ctx.CurrentMethod.Owner, id6, id7);
            // four order: static field
            if (flags.HasFlag(AccessFlags.STATIC_FIELD))
                return gen.EmitLoadField(ctx.CurrentMethod.Owner, id6, id7);

            // five order: prop
            if (flags.HasFlag(AccessFlags.PROPERTY))
                return gen.EmitThis().EmitCallProperty(ctx.CurrentMethod.Owner, id6, id7);
            // six order: static prop
            if (flags.HasFlag(AccessFlags.STATIC_PROPERTY))
                return gen.EmitCallProperty(ctx.CurrentMethod.Owner, id6, id7);

            // seven order: static class
            if (flags.HasFlag(AccessFlags.CLASS))
            {
                var @class = ctx.ResolveType(id6);
                return gen.EmitLoadField(@class, id7);
            }

            return gen;
        }

        if (access is IndexerAccessExpressionSyntax { Right: ArgumentListExpression args, Left: IdentifierExpression accessId })
        {
            var argTypes = args.Arguments.DetermineTypes(ctx).ToList();

            var flags = gen.GetAccessFlags(accessId);

            VeinMethod method;

            // first order: variable
            if (flags.HasFlag(AccessFlags.VARIABLE))
            {
                var (varType, var_index) = ctx.CurrentScope.GetVariable(accessId);

                if (varType.IsGeneric)
                    throw new NotSupportedException($"Temporarily is not possible to get method declaration from generic type");

                method = varType.Class.FindMethod(VeinArray.ArrayAccessGetterMethodName, argTypes);

                gen.EmitLoadLocal(var_index);
            }
            // second order: argument
            else if (flags.HasFlag(AccessFlags.ARGUMENT))
            {
                var (arg, index) = ctx.GetCurrentArgument(accessId);

                method = arg.Type.FindMethod(VeinArray.ArrayAccessGetterMethodName, argTypes);

                gen.EmitLoadArgument(index);
            }
            // three order: field
            else if (flags.HasFlag(AccessFlags.FIELD))
            {
                var type = ctx.ResolveField(accessId);

                method = type.FieldType.Class.FindMethod(VeinArray.ArrayAccessGetterMethodName, argTypes);

                if (method is null)
                    throw new InvalidOperationException();

                gen.EmitThis().EmitLoadField(ctx.CurrentMethod.Owner, accessId);
            }
            else
                throw new NotSupportedException();

            foreach (var ar in args.Arguments)
                gen.EmitExpression(ar);

            gen.Emit(OpCodes.CALL, method);

            return gen;
        }

        throw new NotSupportedException();
    }
}
