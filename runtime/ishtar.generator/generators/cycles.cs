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
        var start = gen.DefineLabel("while-start");
        var end = gen.DefineLabel("while-end");
        var expType = @while.Expression.DetermineType(ctx);
        gen.UseLabel(start);
        if (expType.TypeCode == VeinTypeCode.TYPE_BOOLEAN)
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

    public static void EmitForStatement(this ILGenerator gen, ForStatementSyntax @for)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        if (@for.LoopVariable is not null)
            gen.EmitLocalVariable(@for.LoopVariable);
        var start = gen.DefineLabel("for-start");
        gen.UseLabel(start);
        gen.EmitStatement(@for.Statement);

        if (@for.LoopCounter is not null)
            gen.EmitExpression(@for.LoopCounter);
        if (@for.LoopContact is not null)
        {
            var expType = @for.LoopContact.DetermineType(ctx);

            if (expType.TypeCode is not VeinTypeCode.TYPE_BOOLEAN)
            {
                ctx.LogError($"Cannot implicitly convert type '{expType}' to 'Boolean'", @for.LoopContact);
                return; // TODO implicit boolean
            }

            gen.EmitExpression(@for.LoopContact);
            gen.Emit(OpCodes.JMP_T, start);
        }
        else
            gen.Emit(OpCodes.JMP, start);
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
