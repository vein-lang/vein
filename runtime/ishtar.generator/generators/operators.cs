namespace ishtar;

using System.Linq.Expressions;
using System;
using emit;
using vein.runtime;
using vein.syntax;
using System.Collections.Generic;
using System.Linq;
using vein;
using vein.extensions;
using static G_Access;

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

        gen.EmitExpression(right);
        gen.EmitExpression(left);

        var left_type_u = left.DetermineType(context);
        var right_type_u = right.DetermineType(context);


        if (left_type_u.IsGeneric)
        {
            context.LogError($"Cannot implicitly convert type '{left_type_u.TypeArg.Name}' to 'Number'", left);
            return;
        }
        if (right_type_u.IsGeneric)
        {
            context.LogError($"Cannot implicitly convert type '{right_type_u.TypeArg.Name}' to 'Number'", right);
            return;
        }

        var left_type = left_type_u.Class;
        var right_type = right_type_u.Class;

        if (left_type.TypeCode.HasNumber() && right_type.TypeCode.HasNumber())
            gen.EmitBinaryOperator(op);
        else if (left_type.TypeCode.HasBoolean() && right_type.TypeCode.HasBoolean())
            gen.EmitBinaryOperator(op);
        else
        {
            var name = $"op_{op}";
            var args = new[] { (VeinComplexType)left_type, (VeinComplexType)right_type };


            var method = left_type.FindMethod(name, args);

            if (method is null)
            {
                context.LogError($"The operator '[red]{op}[/]' not implemented in '{left_type.FullName}'.", left);
                throw new SkipStatementException();
            }


            var methodName = VeinMethod.GetFullName(name, method.ReturnType, args);

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

            if (accessTag is AccessFlags.VARIABLE)
            {
                var (clazz, slot) = context.CurrentScope.GetVariable(id);
                gen.EmitExpression(bin.Right);
                gen.Emit(OpCodes.STLOC_S, slot);
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
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (node.OperatorType == ExpressionType.NegateChecked && node.Operand.GetTypeCode().HasInteger())
        {
            var type = node.Operand.GetTypeCode();
            gen.EmitDefault(type);
            gen.EmitExpression(node.Operand);
            gen.EmitBinaryOperator(ExpressionType.SubtractChecked, type, type, type);
        }
        else if (node.OperatorType == ExpressionType.Negate && node.Operand.GetTypeCode().HasInteger())
        {
            // yes I be known, its fucking shit
            if (node.Operand is Int32LiteralExpressionSyntax int32)
            {
                int32.ExpressionString = $"-{int32.ExpressionString}";
                gen.EmitExpression(int32);
            }
            else
                throw new NotSupportedException();
        }
        else if (node.OperatorType == ExpressionType.Negate && node.Operand is AccessExpressionSyntax access)
        {
            gen.EmitExpression(new Int32LiteralExpressionSyntax(-1));
            gen.EmitExpression(access);
            gen.Emit(OpCodes.MUL);
        }
        else if (node.OperatorType == ExpressionType.Not)
        {
            gen.EmitExpression(node.Operand);
            gen.Emit(OpCodes.LDC_I4_0);
            gen.Emit(OpCodes.EQL_NQ);
        }
        else if (node.OperatorType is ExpressionType.PostIncrementAssign)
        {
            // todo work only in for cycle, in other cases, it works like ++i
            var addOne =
                new BinaryExpressionSyntax(node.Operand, new Int32LiteralExpressionSyntax(1).SetPos<Int32LiteralExpressionSyntax>(node.Transform), ExpressionType.Add);
            var operand_assign = new BinaryExpressionSyntax(node.Operand, addOne).SetPos<BinaryExpressionSyntax>(node.Transform);
            gen.EmitAssignExpression(operand_assign);
        }
        else if (node.OperatorType is ExpressionType.PostDecrementAssign)
        {
            // todo work only in for cycle, in other cases, it works like --i
            var subOne =
                new BinaryExpressionSyntax(node.Operand, new Int32LiteralExpressionSyntax(1).SetPos<Int32LiteralExpressionSyntax>(node.Transform), ExpressionType.Subtract);
            var operand_assign = new BinaryExpressionSyntax(node.Operand, subOne).SetPos<BinaryExpressionSyntax>(node.Transform);
            gen.EmitAssignExpression(operand_assign);
        }
        else if (node.OperatorType == ExpressionType.And && node.Operand.GetTypeCode(ctx) == VeinTypeCode.TYPE_FUNCTION)
        {
            var fnType = node.Operand.GetType(ctx);

            var invokeMethod = fnType.FindMethod("invoke");


            gen.Emit(OpCodes.NEWOBJ, fnType);

            if (node.Operand is IdentifierExpression id)
            {
                var method = ctx.CurrentMethod.Owner.FindMethod(id.ExpressionString, invokeMethod.Signature.Arguments.Select(x => x.ComplexType));

                gen.Emit(OpCodes.LDFN, method);
            }
            else
                throw new NotSupportedException("EmitLoadFunction");
            gen.Emit(OpCodes.LDNULL);

            var ctor = fnType.FindMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR,
            [
                fnType,
                VeinTypeCode.TYPE_RAW.AsClass(ctx.Module.Types),
                VeinTypeCode.TYPE_OBJECT.AsClass(ctx.Module.Types)
            ], true);



            gen.Emit(OpCodes.CALL, ctor);
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
                gen.Emit(OpCodes.EQL_NN);
                return;
            case ExpressionType.Equal:
                gen.Emit(OpCodes.EQL_NQ);
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
