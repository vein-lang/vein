namespace ishtar;

using System.Linq.Expressions;
using System;
using emit;
using vein.runtime;
using vein.syntax;
using static ishtar.G_Access;
using System.Collections.Generic;
using System.Linq;
using vein;
using vein.extensions;

public static class G_Operators
{
    public static void EmitBinaryExpression(this ILGenerator gen, BinaryExpressionSyntax bin)
    {
        if (bin.OperatorType == ExpressionType.Assign)
        {
            gen.EmitAssignExpression(bin);
            return;
        }

        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        var left = bin.Left;
        var right = bin.Right;
        var op = bin.OperatorType;

        gen.EmitExpression(left);
        gen.EmitExpression(right);

        var left_type = left.DetermineType(context);
        var right_type = right.DetermineType(context);

        if (left_type.TypeCode.HasNumber() && right_type.TypeCode.HasNumber())
            gen.EmitBinaryOperator(op);
        else
        {
            var name = $"op_{op}";
            var args = new[] { left_type, right_type };

            var methodName = VeinMethodBase.GetFullName(name, args);

            var method = left_type.FindMethod(name, args);
            if (method is null || !method.IsStatic || !method.IsSpecial)
            {
                context.LogError($"Operator '{op.GetSymbol()}' " +
                                 $"cannot be applied to operand of type '{left_type.Name}' and '{right_type.Name}'.", bin);
                context.LogError($"Not found definition for '{op.GetSymbol()}' operator in '{left_type.Name}'. [{methodName}]", bin);
                throw new SkipStatementException();
            }

            gen.Emit(OpCodes.CALL, method);
        }
    }

    public static void EmitAssignExpression(this ILGenerator gen, BinaryExpressionSyntax bin)
    {
        var context = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (bin.Left is IdentifierExpression id)
        {
            var accessTag = gen.GetAccessFlags(id);

            if (accessTag is AccessFlags.FIELD or AccessFlags.STATIC_FIELD)
            {
                var field = context.ResolveField(context.CurrentMethod.Owner, id);
                gen.EmitExpression(bin.Right).EmitThis(accessTag is AccessFlags.FIELD).EmitStageField(field);
                return;
            }

            if (accessTag is AccessFlags.PROPERTY or AccessFlags.STATIC_PROPERTY)
            {
                var prop = context.ResolveProperty(context.CurrentMethod.Owner, id);
                gen.EmitThis(accessTag is AccessFlags.PROPERTY)
                    .EmitExpression(bin.Right)
                    .Emit(OpCodes.CALL, prop.Setter);
                return;
            }

            context.LogError($"Member '{id}' is not found in '{gen._methodBuilder.classBuilder.Owner.Name}'.", id);
            throw new SkipStatementException();
        }

        if (bin is { Left: AccessExpressionSyntax { Left: ThisAccessExpression, Right: IdentifierExpression id1 } })
        {
            var accessTag = gen.GetAccessFlags(id1);

            if (accessTag == AccessFlags.PROPERTY)
            {
                var prop = context.ResolveProperty(id1);
                gen.EmitThis().EmitExpression(bin.Right).Emit(OpCodes.CALL, prop.Setter);
                return;
            }
            if (accessTag == AccessFlags.FIELD)
            {
                var field = context.ResolveField(context.CurrentMethod.Owner, id1);
                if (field.IsStatic)
                {
                    context.LogError($"Member '{id1}' cannot be accessed with an instance reference.", id1);
                    throw new SkipStatementException();
                }
                gen.EmitExpression(bin.Right).EmitThis().EmitStageField(field);
                return;
            }
            context.LogError($"Member '{id1}' is not found in '{gen._methodBuilder.classBuilder.Owner.Name}'.", id1);
            throw new SkipStatementException();
        }

        if (bin is { Left: AccessExpressionSyntax { Left: SelfAccessExpression, Right: IdentifierExpression id3 } })
        {
            var accessTag = gen.GetAccessFlags(id3);

            if (accessTag == AccessFlags.STATIC_PROPERTY)
            {
                var prop = context.ResolveProperty(id3);
                gen.EmitExpression(bin.Right).Emit(OpCodes.CALL, prop.Setter);
                return;
            }
            if (accessTag == AccessFlags.STATIC_FIELD)
            {
                var field = context.ResolveField(context.CurrentMethod.Owner, id3);
                gen.EmitExpression(bin.Right).EmitStageField(field);
                return;
            }
            context.LogError($"Static member '{id3}' is not found in '{gen._methodBuilder.classBuilder.Owner.Name}'.", id3);
            throw new SkipStatementException();
        }

        if (bin is { Left: AccessExpressionSyntax access, Right: IdentifierExpression id2 })
        {
            bool shot_load_self(bool yes = false)
            {
                if (!yes) return false;
                if (context.CurrentMethod.IsStatic)
                    return false;

                gen.Emit(OpCodes.LDARG_0); // load this
                return false;
            }
            bool need_load_self = true;
            var visitedNodes = new List<BaseSyntax>();
            var targetClass = context.CurrentMethod.Owner;

            foreach (var (node, index, tag) in access.ChildNodes.Tagget())
            {
                visitedNodes.Add(node);
                if (node is not IdentifierExpression node_id)
                {
                    context.LogError($"Member '{visitedNodes.Select(x => $"{x}").Join(".")}' " +
                                     $"cannot be accessed with an instance reference; qualify it with a type name instead", access);
                    throw new SkipStatementException();
                }

                if (tag.isFirst)
                {
                    var accessTag = gen.GetAccessFlags(node_id);

                    if (accessTag is AccessFlags.FIELD or AccessFlags.STATIC_FIELD)
                    {
                        var field = context.ResolveField(targetClass, node_id);
                        need_load_self = shot_load_self(need_load_self);
                        gen.Emit(field.IsStatic ? OpCodes.LDSF : OpCodes.LDF, field);
                        targetClass = field.FieldType;
                        continue;
                    }

                    if (accessTag is AccessFlags.PROPERTY or AccessFlags.STATIC_PROPERTY)
                    {
                        var prop = context.ResolveProperty(targetClass, node_id);
                        need_load_self = shot_load_self(need_load_self);
                        gen.Emit(OpCodes.CALL, prop.Getter);
                        targetClass = prop.PropType;
                        continue;
                    }

                    if (accessTag == AccessFlags.ARGUMENT)
                    {
                        var (arg, slot) = context.GetCurrentArgument(node_id);
                        gen.EmitLoadArgument(slot);
                        targetClass = arg.Type;
                        continue;
                    }

                    if (accessTag == AccessFlags.CLASS)
                    {
                        targetClass = context.ResolveType(node_id);
                        continue;
                    }
                    if (accessTag == AccessFlags.VARIABLE)
                    {
                        var (clazz, slot) = context.CurrentScope.GetVariable(node_id);
                        gen.EmitLoadLocal(slot);
                        continue;
                    }
                    throw new SkipStatementException();
                }
                else
                {
                    var field = context.ResolveField(targetClass, node_id);

                    targetClass = field.FieldType;

                    if (tag.isLast)
                        gen.EmitExpression(id2).Emit(OpCodes.STF);
                    else
                        gen.Emit(OpCodes.LDF, field);
                }
            }

            return;
        }

        throw new NotSupportedException();
    }

    public static ILGenerator EmitUnary(this ILGenerator gen, UnaryExpressionSyntax node)
    {
        if (node.OperatorType == ExpressionType.NegateChecked && node.Operand.GetTypeCode().HasInteger())
        {
            var type = node.Operand.GetTypeCode();
            gen.EmitDefault(type);
            gen.EmitExpression(node.Operand);
            gen.EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type);
        }
        else if (node.OperatorType == ExpressionType.Not)
        {
            gen.EmitExpression(node.Operand);
            gen.Emit(OpCodes.LDC_I4_0);
            gen.Emit(OpCodes.EQL_T);
        }
        else
            throw new NotSupportedException("EmitUnary");

        return gen;
    }


    public static void EmitBinaryOperator(this ILGenerator gen, ExpressionType op, VeinTypeCode leftType = VeinTypeCode.TYPE_CLASS, VeinTypeCode rightType = VeinTypeCode.TYPE_CLASS, VeinTypeCode resultType = VeinTypeCode.TYPE_CLASS)
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
            case ExpressionType.LessThan:
                gen.Emit(OpCodes.EQL_L);
                return;
            case ExpressionType.LessThanOrEqual:
                gen.Emit(OpCodes.EQL_LQ);
                return;
            case ExpressionType.GreaterThan:
                gen.Emit(OpCodes.EQL_H);
                return;
            case ExpressionType.GreaterThanOrEqual:
                gen.Emit(OpCodes.EQL_HQ);
                return;
            case ExpressionType.NotEqual:
                if (leftType == VeinTypeCode.TYPE_BOOLEAN)
                    goto case ExpressionType.ExclusiveOr;
                gen.Emit(OpCodes.EQL_F);
                return;
            case ExpressionType.Equal:
                gen.Emit(OpCodes.EQL_T);
                return;
            case ExpressionType.ExclusiveOr:
                gen.Emit(OpCodes.XOR);
                return;
            case ExpressionType.Modulo:
                gen.Emit(OpCodes.MOD);
                return;
            case ExpressionType.LeftShift:
                gen.Emit(OpCodes.SHL);
                return;
            case ExpressionType.RightShift:
                gen.Emit(OpCodes.SHR);
                return;
            default:
                throw new NotSupportedException($"{op} is not currentrly support.");
        }
    }

}
