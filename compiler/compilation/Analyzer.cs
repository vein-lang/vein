namespace vein.compilation;

using ishtar;
using Spectre.Console;
using syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    private void AnalyzeStatement(BaseSyntax statement, DocumentDeclaration doc)
    {
        if (statement is not IPassiveParseTransition { IsBrokenToken: true } transition)
            return;
        var pos = statement.Transform.pos;
        var e = transition.Error;
        var diff_err = statement.Transform.DiffErrorFull(doc);
        Log.errors.Enqueue($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                           $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                           $"in '[orange bold]{doc.FileEntity}[/]'." +
                           $"{diff_err}");
    }
}
