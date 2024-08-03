namespace ishtar;

using emit;
using vein.runtime;
using vein.syntax;

public static class G_Local
{
    public static ILGenerator EmitLoadLocal(this ILGenerator gen, int i) => i switch
    {
        0 => gen.Emit(OpCodes.LDLOC_0),
        1 => gen.Emit(OpCodes.LDLOC_1),
        2 => gen.Emit(OpCodes.LDLOC_2),
        3 => gen.Emit(OpCodes.LDLOC_3),
        4 => gen.Emit(OpCodes.LDLOC_4),
        5 => gen.Emit(OpCodes.LDLOC_5),
        _ => gen.Emit(OpCodes.LDLOC_S, i)
    };

    public static ILGenerator EmitLoadField(this ILGenerator gen, VeinClass @class, params IdentifierExpression[] chain)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var clazz = @class;

        foreach (var id in chain)
        {
            var field = clazz.FindField($"{id}");

            if (field is null)
            {
                ctx.LogError($"Field '{id}' is not found in '{clazz.Name}' class.", id);
                throw new SkipStatementException();
            }

            if (field.IsStatic)
                gen.Emit(OpCodes.LDSF, field);
            else
                gen.Emit(OpCodes.LDF, field);
            clazz = field.FieldType;
        }

        return gen;
    }

    public static void EmitLocalVariableWithType(this ILGenerator generator, LocalVariableDeclaration localVar, VeinClass type)
    {
        var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
        var scope = ctx.CurrentScope;

        if (!localVar.Body.IsEmpty)
        {
            ctx.LogError($"Local variable already defined type.", localVar);
            return;
        }

        var locIndex = generator.EnsureLocal(localVar.Identifier.ExpressionString, type);

        if (locIndex < 0)
        {
            ctx.LogError($"Too many variables in '{ctx.CurrentMethod.Name}' function.", localVar);
            return;
        }

        scope.DefineVariable(localVar.Identifier, type, locIndex);

        generator.Emit(OpCodes.LDNULL);
        generator.Emit(OpCodes.STLOC_S, locIndex); // TODO optimization for STLOC_0,1,2 and etc
    }

    public static void EmitLocalVariable(this ILGenerator generator, LocalVariableDeclaration localVar)
    {
        var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
        var scope = ctx.CurrentScope;

        if (localVar.Body.IsEmpty)
        {
            ctx.LogError($"Implicitly-typed local variable must be initialized.", localVar);
            return;
        }

        var exp = localVar.Body.Get();
        var type = exp.DetermineType(scope.Context);

        if (type.IsGeneric)
        {

        }

        var locIndex = generator.EnsureLocal(localVar.Identifier.ExpressionString, type);

        if (locIndex < 0)
        {
            ctx.LogError($"Too many variables in '{ctx.CurrentMethod.Name}' function.", localVar);
            return;
        }

        scope.DefineVariable(localVar.Identifier, type, locIndex);

        generator.EmitExpression(exp);
        generator.Emit(OpCodes.STLOC_S, locIndex); // TODO optimization for STLOC_0,1,2 and etc
    }
}
