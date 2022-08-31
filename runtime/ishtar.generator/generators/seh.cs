namespace ishtar;

using System.Collections.Generic;
using emit;
using vein.syntax;

public static class G_SEH
{
    public static void EmitFail(this ILGenerator generator, FailStatementSyntax syntax)
    {
        generator.EmitExpression(syntax.Expression);
        generator.Emit(OpCodes.THROW);
    }

    public static ILGenerator EmitTry(this ILGenerator gen, TryStatementSyntax @try)
    {
        gen.BeginTryBlock();
        gen.EmitBlock(@try.TryBlock);

        gen.EmitCatches(@try.Catches);
        gen.EmitFinally(@try.Finally);

        gen.EndExceptionBlock();

        return gen;
    }

    public static void EmitFinally(this ILGenerator gen, FinallyClauseSyntax @finally)
    {
        if (@finally is null) return;
        gen.BeginFinallyBlock();
        gen.EmitBlock(@finally.Block);
    }

    public static void EmitCatches(this ILGenerator gen, IEnumerable<CatchClauseSyntax> cathes)
    {
        if (cathes is null)
            return;
        foreach (var @catch in cathes)
            gen.EmitCatch(@catch);
    }

    public static void EmitCatch(this ILGenerator gen, CatchClauseSyntax @catch)
    {
        var ctx = gen.ConsumeFromMetadata<GeneratorContext>("context");

        if (@catch.Specifier is null)
        {
            gen.BeginCatchBlock(null);
            gen.EmitBlock(@catch.Block);
            return;
        }

        var filterType = ctx.ResolveType(@catch.Specifier.Type);
        var catchId = gen.BeginCatchBlock(filterType);
        using var catchScope = ctx.CurrentScope.EnterScope();

        if (@catch.Specifier.Identifier.IsDefined)
        {
            var id = @catch.Specifier.Identifier.GetOrDefault();
            ctx.CurrentScope.EnsureExceptionLocal(gen, id, catchId, filterType);
        }
        else
            gen.Emit(OpCodes.POP); // clear exception ref in stack

        foreach (var v in @catch.Block.Statements) gen.EmitStatement(v);
    }
}
