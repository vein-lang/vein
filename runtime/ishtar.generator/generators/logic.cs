namespace ishtar;

using System;
using emit;
using vein.runtime;
using vein.syntax;

public static class G_Logic
{
    public static void EmitIfElse(this ILGenerator generator, IfStatementSyntax ifStatement)
    {
        var elseLabel = generator.DefineLabel("else");
        var endLabel = generator.DefineLabel("if-end");
        var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
        var expType = ifStatement.Expression.DetermineType(ctx);

        if (!ctx.DisableOptimization && ifStatement.Expression is BoolLiteralExpressionSyntax @bool)
        {
            if (@bool.Value)
            {
                generator.EmitStatement(ifStatement.ThenStatement);
                if (ifStatement.ElseStatement is not null)
                    generator.Emit(OpCodes.JMP, endLabel);
            }
            else
                generator.Emit(OpCodes.JMP, elseLabel);
        }
        else if (expType.TypeCode == VeinTypeCode.TYPE_BOOLEAN)
        {
            generator.EmitExpression(ifStatement.Expression);
            generator.Emit(OpCodes.JMP_F, elseLabel);
            generator.EmitStatement(ifStatement.ThenStatement);
            if (ifStatement.ElseStatement is not null)
                generator.Emit(OpCodes.JMP, endLabel);
        }
        else
        {
            ctx.LogError($"Cannot implicitly convert type '{expType}' to 'Boolean'", ifStatement.Expression);
            return;
        }
        generator.UseLabel(elseLabel);

        if (ifStatement.ElseStatement is null)
            return;

        generator.EmitStatement(ifStatement.ElseStatement);
        generator.UseLabel(endLabel);
    }

    public static void EmitStatement(this ILGenerator generator, StatementSyntax statement)
    {
        if (statement.IsBrokenToken)
            return;

        if (statement is ReturnStatementSyntax ret1)
            generator.EmitReturn(ret1);
        else if (statement is SingleStatementSyntax single)
            generator.EmitExpression(single.Expression);
        else if (statement is IfStatementSyntax theIf)
            generator.EmitIfElse(theIf);
        else if (statement is QualifiedExpressionStatement { Value: InvocationExpression invoke })
        {
            var ctx = generator.ConsumeFromMetadata<GeneratorContext>("context");
            generator.EmitCall(ctx.CurrentMethod.Owner, invoke);
        }
        else if (statement is QualifiedExpressionStatement { Value: AccessExpressionSyntax access })
            generator.EmitAccess(access);
        else if (statement is WhileStatementSyntax @while)
            generator.EmitWhileStatement(@while);
        else if (statement is ForStatementSyntax @for)
            generator.EmitForStatement(@for);
        else if (statement is QualifiedExpressionStatement { Value: BinaryExpressionSyntax } qes2)
            generator.EmitBinaryExpression((BinaryExpressionSyntax)qes2.Value);

        else if (statement is LocalVariableDeclaration localVariable)
            generator.EmitLocalVariable(localVariable);
        else if (statement is ForeachStatementSyntax @foreach)
            generator.EmitForeach(@foreach);
        else if (statement is BlockSyntax block)
            generator.EmitBlock(block);
        else if (statement is FailStatementSyntax fail)
            generator.EmitFail(fail);
        else if (statement is TryStatementSyntax @try)
            generator.EmitTry(@try);
        else
            throw new NotImplementedException();
    }
}
