namespace ishtar;

using System;
using emit;
using vein.runtime;
using vein.syntax;

public static class G_Cycles
{
    public static void EmitWhileStatement(this ILGenerator gen, WhileStatementSyntax @while)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var start = gen.DefineLabel();
        var end = gen.DefineLabel();
        var expType = @while.Expression.DetermineType(ctx);
        gen.UseLabel(start);
        if (expType.FullName == VeinTypeCode.TYPE_BOOLEAN.AsClass()(Types.Storage).FullName)
        {
            gen.EmitExpression(@while.Expression);
            gen.Emit(OpCodes.JMP_F, end);
        }
        else // todo implicit boolean
        {
            ctx.LogError($"Cannot implicitly convert type '{expType}' to 'Boolean'", @while.Expression);
            return;
        }
        gen.EmitStatement(@while.Statement);
        gen.Emit(OpCodes.JMP, start);
        gen.UseLabel(end);
    }

    public static void EmitForeach(this ILGenerator generator, ForeachStatementSyntax @foreach)
    {
        var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");

        var type = @foreach.Expression.DetermineType(ctx);

        generator.EmitLocalVariableWithType(@foreach.Variable, type);
        using (ctx.CurrentScope.EnterScope())
        {
            // TODO
        }

        throw new NotSupportedException("Currently foreach is not support");
    }
}
