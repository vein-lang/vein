namespace insomnia.extensions
{
    using System;
    using System.Linq.Expressions;
    using emit;
    using syntax;
    
    public static class GeneratorExtension
    {
        public static void EmitUnary(this ILGenerator gen, UnaryExpressionSyntax node)
        {
            if (node.OperatorType == ExpressionType.NegateChecked && node.Operand.GetTypeCode().HasInteger())
            {
                var type = node.Operand.GetTypeCode();
                gen.EmitDefault(type);
                gen.EmitExpression(node.Operand);
                gen.EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type);
            }
            else
            {
                gen.EmitExpression(node.Operand);
                //gen.EmitUnaryOperator(node.NodeType, node.Operand.Type, node.Type);
            }
        }
        
        
        public static void EmitBinaryOperator(this ILGenerator gen, ExpressionType op, WaveTypeCode leftType, WaveTypeCode rightType, WaveTypeCode resultType)
        {
            switch (op)
            {
                case ExpressionType.Add:
                    gen.Emit(OpCodes.ADD);
                    break;
                case ExpressionType.Subtract:
                    gen.Emit(OpCodes.SUB);
                    break;
                case ExpressionType.Multiply:
                    gen.Emit(OpCodes.MUL);
                    break;
                case ExpressionType.Divide:
                    gen.Emit(OpCodes.DIV);
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    gen.Emit(OpCodes.AND);
                    return;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    gen.Emit(OpCodes.OR);
                    return;
                case ExpressionType.NotEqual:
                    if (leftType == WaveTypeCode.TYPE_BOOLEAN)
                        goto case ExpressionType.ExclusiveOr;
                    gen.Emit(OpCodes.EQL);
                    gen.Emit(OpCodes.LDC_I4_0);
                    goto case ExpressionType.Equal;
                case ExpressionType.Equal:
                    gen.Emit(OpCodes.EQL);
                    return;
                case ExpressionType.ExclusiveOr:
                    gen.Emit(OpCodes.XOR);
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void EmitDefault(this ILGenerator gen, WaveTypeCode code)
        {
            switch (code)
            {
                case WaveTypeCode.TYPE_OBJECT:
                case WaveTypeCode.TYPE_STRING:
                    gen.Emit(OpCodes.LDNULL);
                    break;
                case WaveTypeCode.TYPE_CHAR:
                case WaveTypeCode.TYPE_I2:
                case WaveTypeCode.TYPE_BOOLEAN:
                    gen.Emit(OpCodes.LDC_I2_0);
                    break;
                case WaveTypeCode.TYPE_I4:
                    gen.Emit(OpCodes.LDC_I4_0);
                    break;
                case WaveTypeCode.TYPE_I8:
                    gen.Emit(OpCodes.LDC_I8_0);
                    break;
                case WaveTypeCode.TYPE_R4:
                    gen.Emit(OpCodes.LDC_F4, .0f);
                    break;
                case WaveTypeCode.TYPE_R8:
                    gen.Emit(OpCodes.LDC_F8, .0d);
                    break;
                default:
                    throw new NotSupportedException($"{code}");
            }
        }

        
        public static WaveTypeCode GetTypeCode(this ExpressionSyntax exp)
        {
            if (exp is NumericLiteralExpressionSyntax num)
                return GetTypeCodeFromNumber(num);
            if (exp is BoolLiteralExpressionSyntax)
                return WaveTypeCode.TYPE_BOOLEAN;
            if (exp is StringLiteralExpressionSyntax)
                return WaveTypeCode.TYPE_STRING;
            if (exp is NullLiteralExpressionSyntax)
                return WaveTypeCode.TYPE_NONE;
            return WaveTypeCode.TYPE_CLASS;
        }
        public static WaveTypeCode GetTypeCodeFromNumber(NumericLiteralExpressionSyntax number) =>
        number switch
        {
            ByteLiteralExpressionSyntax => WaveTypeCode.TYPE_U1,
            SByteLiteralExpressionSyntax => WaveTypeCode.TYPE_I1,
            Int16LiteralExpressionSyntax => WaveTypeCode.TYPE_I2,
            UInt16LiteralExpressionSyntax => WaveTypeCode.TYPE_U2,
            Int32LiteralExpressionSyntax => WaveTypeCode.TYPE_I4,
            UInt32LiteralExpressionSyntax => WaveTypeCode.TYPE_U4,
            Int64LiteralExpressionSyntax => WaveTypeCode.TYPE_I8,
            UInt64LiteralExpressionSyntax => WaveTypeCode.TYPE_U8,
            HalfLiteralExpressionSyntax => WaveTypeCode.TYPE_R2,
            SingleLiteralExpressionSyntax => WaveTypeCode.TYPE_R4,
            DoubleLiteralExpressionSyntax => WaveTypeCode.TYPE_R8,
            DecimalLiteralExpressionSyntax => WaveTypeCode.TYPE_R16,
            _ => throw new NotSupportedException($"{number} is not support number.")
        };
        
        public static void EmitExpression(this ILGenerator gen, ExpressionSyntax expr)
        {
            if (expr is LiteralExpressionSyntax literal)
            {
                gen.EmitLiteral(literal);
                return;
            }

            if (expr is NewExpressionSyntax @new)
            {
                gen.EmitCreateObject(@new);
                return;
            }

            throw new NotImplementedException();
        }

        public static void EmitCreateObject(this ILGenerator gen, NewExpressionSyntax @new)
        {
            var type = gen._methodBuilder.moduleBuilder.FindType(@new.TargetType.Typeword.Identifier,
                gen._methodBuilder.classBuilder.Includes);
            var module = gen._methodBuilder.moduleBuilder;
            foreach (var arg in @new.CtorArgs) 
                gen.EmitExpression(arg);
            gen.Emit(OpCodes.NEWOBJ, type);

            var ctor = type.FindMethod("ctor", @new.CtorArgs.DetermineTypes(module));

            gen.Emit(OpCodes.CALL, ctor);
        }

        public static void EmitThrow(this ILGenerator generator, QualityTypeName type)
        {
            generator.Emit(OpCodes.NEWOBJ, type);
            generator.Emit(OpCodes.CALL, GetDefaultCtor(type));
            generator.Emit(OpCodes.THROW);
        }
        public static WaveMethod GetDefaultCtor(QualityTypeName t) => throw new NotImplementedException();
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