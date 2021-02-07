namespace wave.extensions
{
    using System;
    using emit;
    using syntax;

    public static class GeneratorExtension
    {
        public static void EmitThrow(this ILGenerator generator, TypeName type)
        {
            generator.Emit(OpCodes.NEWOBJ, type.Token.Value);
            generator.EmitCall(OpCodes.CALL, GetDefaultCtor(type));
            generator.Emit(OpCodes.THROW);
        }
        public static WaveClassMethod GetDefaultCtor(TypeName t) => throw new NotImplementedException();
        public static void EmitIfElse(this ILGenerator generator, IfStatementSyntax ifStatement)
        {
            var elseLabel = generator.DefineLabel();
            
            if (ifStatement.Expression is BoolLiteralExpressionSyntax @bool)
            {
                if (@bool.Value)
                    generator.EmitStatement(ifStatement.ThenStatement);
                else
                    generator.Emit(OpCodes.JMP, elseLabel);
            }
            
            
            
            generator.UseLabel(elseLabel);
            if (ifStatement.ElseStatement is ReturnStatementSyntax ret2)
                generator.EmitReturn(ret2);
            else
                throw new NotImplementedException();
        }

        public static void EmitStatement(this ILGenerator generator, StatementSyntax statement)
        {
            if (statement is ReturnStatementSyntax ret1)
                generator.EmitReturn(ret1);
            else
                throw new NotImplementedException();
        }
        public static void EmitNumericLiteral(this ILGenerator generator, NumericLiteralExpressionSyntax literal)
        {
            switch (literal)
            {
                case Int16LiteralExpressionSyntax i16:
                {
                    if (i16 is {Value: 0}) generator.Emit(OpCodes.LDC_I2_0);
                    if (i16 is {Value: 1}) generator.Emit(OpCodes.LDC_I2_1);
                    if (i16 is {Value: 2}) generator.Emit(OpCodes.LDC_I2_2);
                    if (i16 is {Value: 3}) generator.Emit(OpCodes.LDC_I2_3);
                    if (i16 is {Value: 4}) generator.Emit(OpCodes.LDC_I2_4);
                    if (i16 is {Value: 5}) generator.Emit(OpCodes.LDC_I2_5);
                    if (i16.Value > 5 || i16.Value < 0)
                        generator.Emit(OpCodes.LDC_I2_S, i16.Value);
                    break;
                }
                case Int32LiteralExpressionSyntax i32:
                {
                    if (i32 is {Value: 0}) generator.Emit(OpCodes.LDC_I4_0);
                    if (i32 is {Value: 1}) generator.Emit(OpCodes.LDC_I4_1);
                    if (i32 is {Value: 2}) generator.Emit(OpCodes.LDC_I4_2);
                    if (i32 is {Value: 3}) generator.Emit(OpCodes.LDC_I4_3);
                    if (i32 is {Value: 4}) generator.Emit(OpCodes.LDC_I4_4);
                    if (i32 is {Value: 5}) generator.Emit(OpCodes.LDC_I4_5);
                    if (i32.Value > 5 || i32.Value < 0) 
                        generator.Emit(OpCodes.LDC_I4_S, i32.Value);
                    break;
                }
                case Int64LiteralExpressionSyntax i64:
                {
                    if (i64 is {Value: 0}) generator.Emit(OpCodes.LDC_I8_0);
                    if (i64 is {Value: 1}) generator.Emit(OpCodes.LDC_I8_1);
                    if (i64 is {Value: 2}) generator.Emit(OpCodes.LDC_I8_2);
                    if (i64 is {Value: 3}) generator.Emit(OpCodes.LDC_I8_3);
                    if (i64 is {Value: 4}) generator.Emit(OpCodes.LDC_I8_4);
                    if (i64 is {Value: 5}) generator.Emit(OpCodes.LDC_I8_5);
                    if (i64.Value > 5 || i64.Value < 0)
                        generator.Emit(OpCodes.LDC_I8_S, i64.Value);
                    break;
                }
                case DecimalLiteralExpressionSyntax f128:
                {
                    generator.Emit(OpCodes.LDC_F16, f128.Value);
                    break;
                }
                case DoubleLiteralExpressionSyntax f64:
                {
                    generator.Emit(OpCodes.LDC_F8, f64.Value);
                    break;
                }
                case SingleLiteralExpressionSyntax f32:
                {
                    generator.Emit(OpCodes.LDC_F4, f32.Value);
                    break;
                }
            }
        }

        public static void EmitLiteral(this ILGenerator generator, LiteralExpressionSyntax literal)
        {
            if (literal is NumericLiteralExpressionSyntax numeric)
                generator.EmitNumericLiteral(numeric);
            else if (literal is StringLiteralExpressionSyntax stringLiteral)
                generator.Emit(OpCodes.LDC_STR, stringLiteral.Value);
            else if (literal is BoolLiteralExpressionSyntax boolLiteral)
                generator.Emit(boolLiteral.Value ? OpCodes.LDC_I2_1 : OpCodes.LDC_I2_0);
            else if (literal is NullLiteralExpressionSyntax)
                generator.Emit(OpCodes.LDNULL);
        }
        
        public static void EmitReturn(this ILGenerator generator, ReturnStatementSyntax statement)
        {
            if (statement is not { Expression: { } })
            {
                generator.Emit(OpCodes.RET);
                return;
            }
            if (statement.Expression is LiteralExpressionSyntax literal)
            {
                generator.EmitLiteral(literal);
                generator.Emit(OpCodes.RET);
                return;
            }

            if (statement.Expression.Kind == SyntaxType.Expression)
            {
                generator.Emit(OpCodes.LDF, new FieldName(statement.Expression.ExpressionString));
                generator.Emit(OpCodes.RET);
                return;
            }
        }
    }
}