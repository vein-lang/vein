namespace ishtar;
#nullable enable
using System;
using System.Linq;
using System.Security.Claims;
using emit;
using vein.runtime;
using vein.syntax;

public static class G_Call
{
    public static ILGenerator EmitCall(this ILGenerator gen, VeinClass @class, InvocationExpression invocation)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (ctx.IsCallingDelegate(invocation, out var argument, out var index))
        {
            gen.EmitLoadArgument(index!.Value);


            foreach (var arg in invocation.Arguments)
                gen.EmitExpression(arg);
            var internalMethod = argument!.Type.FindMethod("invoke") ;

            if (internalMethod is null)
            {
                ctx.LogError($"Bad '{argument!.Type.FullName}' function class.", invocation);
                throw new SkipStatementException();
            }

            gen.Emit(OpCodes.CALL, internalMethod);
            return gen;
        }


        var method = ctx.ResolveMethod(@class, invocation);
        var args = invocation.Arguments;

        foreach (var arg in args)
            gen.EmitExpression(arg);

        gen.Emit(OpCodes.CALL, method);

        if (method.ReturnType.IsGeneric)
        {
            if (invocation.NoReturn)
                gen.Emit(OpCodes.POP);
        }
        else if (method.ReturnType.Class.TypeCode != VeinTypeCode.TYPE_VOID)
        {
            if (invocation.NoReturn)
                gen.Emit(OpCodes.POP);
        }

        return gen;
    }

    public static bool IsCallingDelegate(this GeneratorContext gen, InvocationExpression invocation, out VeinArgumentRef? arg, out int? index)
    {
        (arg, index) = gen.CurrentMethod.Signature.Arguments
            .Select((x, y) => (x, y))
            .Where((x) => !x.x.IsGeneric)
            .SingleOrDefault(x => x.x.Name.Equals(invocation.Name.ExpressionString));

        if (arg is null)
            return false;
        if (arg.IsGeneric)
            return false;
        if (arg.Type.TypeCode is VeinTypeCode.TYPE_FUNCTION)
            return true;
        return false;
    }

    public static ILGenerator EmitCallProperty(this ILGenerator gen, VeinClass @class, params IdentifierExpression[] chain)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var clazz = @class;

        foreach (var id in chain)
        {
            var prop = clazz.FindProperty($"{id}");

            if (prop is null)
            {
                ctx.LogError($"Propperty '{id}' is not found in '{clazz.Name}' class.", id);
                throw new SkipStatementException();
            }

            if (prop.IsStatic)
                gen.Emit(OpCodes.CALL, prop.Getter);
            else
                gen.EmitThis().Emit(OpCodes.CALL, prop.Getter);
            clazz = prop.PropType;
        }

        return gen;
    }
}
