namespace ishtar;

using emit;
using vein.runtime;
using vein.syntax;
using static G_Access;

public static class G_Unsafe
{
    public static void EmitUnsafeBlock(this ILGenerator gen, UnsafeBlockSyntax unsafeBlock)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");
        var prev = ctx.IsUnsafeContext;
        ctx.IsUnsafeContext = true;
        gen.EmitBlock(unsafeBlock.Body);
        ctx.IsUnsafeContext = prev;
    }

    public static ILGenerator EmitStackAlloc(this ILGenerator gen, StackAllocExpressionSyntax expr)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (!ctx.IsUnsafeContext)
        {
            ctx.LogError("stackalloc is only allowed in an unsafe context", expr);
            throw new SkipStatementException();
        }

        var elementType = ctx.ResolveType(expr.ElementType);

        if (elementType.IsGeneric)
        {
            ctx.LogError("stackalloc cannot be used with generic types", expr);
            throw new SkipStatementException();
        }

        var elemSize = elementType.Class.TypeCode.GetNativeSize();

        if (elemSize <= 0)
        {
            ctx.LogError($"stackalloc element type '{elementType.Class.Name}' has unknown or zero size", expr);
            throw new SkipStatementException();
        }

        gen.EmitExpression(expr.Count);

        if (elemSize > 1)
        {
            gen.Emit(OpCodes.LDC_I4_S, (int)elemSize);
            gen.Emit(OpCodes.MUL);
        }

        gen.Emit(OpCodes.STACKALLOC);

        return gen;
    }

    public static ILGenerator EmitAddressOf(this ILGenerator gen, UnaryExpressionSyntax node)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (!ctx.IsUnsafeContext)
        {
            ctx.LogError("Address-of operator '&' is only allowed in an unsafe context", node);
            throw new SkipStatementException();
        }

        if (node.Operand is not IdentifierExpression id)
        {
            ctx.LogError("Address-of operator '&' requires a local variable or argument as operand", node);
            throw new SkipStatementException();
        }

        var accessTag = gen.GetAccessFlags(id);

        if (accessTag == AccessFlags.VARIABLE)
        {
            var (_, slot) = ctx.CurrentScope.GetVariable(id);
            gen.Emit(OpCodes.ADDR_LOC, slot);
            return gen;
        }

        if (accessTag == AccessFlags.ARGUMENT)
        {
            var (_, slot) = ctx.GetCurrentArgument(id);
            gen.Emit(OpCodes.ADDR_ARG, slot);
            return gen;
        }

        ctx.LogError($"Cannot take the address of '{id}': only local variables and method arguments are supported", node);
        throw new SkipStatementException();
    }

    public static ILGenerator EmitPointerDereference(this ILGenerator gen, UnaryExpressionSyntax node)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (!ctx.IsUnsafeContext)
        {
            ctx.LogError("Pointer dereference operator '*' is only allowed in an unsafe context", node);
            throw new SkipStatementException();
        }

        var ptrType = node.Operand.DetermineType(ctx);

        if (!ptrType.IsGeneric && ptrType.Class.TypeCode != VeinTypeCode.TYPE_RAW)
        {
            ctx.LogError($"Cannot dereference non-pointer type '{ptrType}'", node);
            throw new SkipStatementException();
        }

        gen.EmitExpression(node.Operand);
        // For v1: default dereference as i32; typed-pointer support requires a richer type system
        gen.Emit(OpCodes.LDIND, (int)VeinTypeCode.TYPE_I4);

        return gen;
    }

    public static ILGenerator EmitPointerStore(this ILGenerator gen, BinaryExpressionSyntax assign,
        UnaryExpressionSyntax ptrDeref)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (!ctx.IsUnsafeContext)
        {
            ctx.LogError("Pointer dereference assignment is only allowed in an unsafe context", assign);
            throw new SkipStatementException();
        }

        gen.EmitExpression(assign.Right);
        gen.EmitExpression(ptrDeref.Operand);
        gen.Emit(OpCodes.STIND, (int)VeinTypeCode.TYPE_I4);

        return gen;
    }
}
