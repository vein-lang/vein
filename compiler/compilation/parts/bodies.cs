namespace vein.compilation;

using ishtar;
using System;
using System.Linq;
using ishtar.emit;
using Spectre.Console;
using vein.syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    public void GenerateBody((MethodBuilder method, MethodDeclarationSyntax member) t)
    {
        if (t == default) return;
        var (method, member) = t;

        GenerateBody(method, member.Body, member.OwnerClass.OwnerDocument);
    }

    public void PostgenerateBody(MethodBuilder method)
    {
        var generator = method.GetGenerator();
        // fucking shit fucking
        // VM needs the end-of-method notation, which is RETURN.
        // but in case of the VOID method, user may not write it
        // and i didnt think of anything smarter than checking last OpCode
        if (!generator._opcodes.Any() && method.ReturnType.TypeCode == TYPE_VOID)
            generator.Emit(OpCodes.RET);
        if (generator._opcodes.Any() && generator._opcodes.Last() != OpCodes.RET.Value && method.ReturnType.TypeCode == TYPE_VOID)
            generator.Emit(OpCodes.RET);
    }

    private void GenerateBody(MethodBuilder method, BlockSyntax block, DocumentDeclaration doc)
    {
        if (block is null)
            return;
        Status.VeinStatus($"Emitting [gray]'{method.Owner.FullName}:{method.Name}'[/]");
        foreach (var pr in block.Statements.SelectMany(x => x.ChildNodes.Concat(new[] { x })))
            AnalyzeStatement(pr, doc);

        if (method.IsAbstract)
            return;

        var generator = method.GetGenerator();
        Context.Document = doc;
        Context.CurrentMethod = method;
        Context.CreateScope();
        generator.StoreIntoMetadata("context", Context);

        foreach (var statement in block.Statements)
        {
            try
            {
                generator.EmitStatement(statement);
            }
            catch (NotSupportedException e)
            {
                Log.Defer.Error($"[red bold]This syntax/statement currently is not supported.[/]", statement,
                    Context.Document);
#if DEBUG
                if (_flags.DisplayStacktraceGenerator)
                    AnsiConsole.WriteException(e);
#endif
            }
            catch (NotImplementedException e)
            {
                Log.Defer.Error($"[red bold]This syntax/statement currently is not implemented.[/]", statement,
                    Context.Document);
#if DEBUG
                if (_flags.DisplayStacktraceGenerator)
                    AnsiConsole.WriteException(e);
#endif
            }
            catch (SkipStatementException e) when (e.IsForceStop)
            {
                throw e;
            }
            catch (SkipStatementException e)
            {
#if DEBUG
                if (_flags.DisplayStacktraceGenerator)
                    AnsiConsole.WriteException(e);
#endif
            }
            catch (Exception e)
            {
                Log.Defer.Error($"[red bold]{e.Message.EscapeMarkup()}[/] in [italic]EmitStatement(...);[/]",
                    statement, Context.Document);
            }
        }
    }
}
