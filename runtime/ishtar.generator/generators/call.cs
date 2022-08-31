namespace ishtar;

using emit;
using vein.runtime;
using vein.syntax;

public static class G_Call
{
    public static ILGenerator EmitCall(this ILGenerator gen, VeinClass @class, InvocationExpression invocation)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        var method = ctx.ResolveMethod(@class, invocation);
        var args = invocation.Arguments;

        foreach (var arg in args)
            gen.EmitExpression(arg);

        gen.Emit(OpCodes.CALL, method);
        return gen;
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
