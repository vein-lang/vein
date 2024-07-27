using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using vein;
using vein.syntax;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

public interface IDiagnosticService
{
    public bool HasDiagnostics(DocumentUri uri, Position position);
    public void Track(DocumentUri uri);
    public void Update(DocumentUri uri, CompilationState state, string source);
    public void Untrack(DocumentUri uri);
}


public class DiagnosticService(ILanguageServerFacade facade) : IDiagnosticService
{
    private readonly Dictionary<DocumentUri, List<Diagnostic>> current = new();
    private bool? supported;

    public bool HasDiagnostics(DocumentUri uri, Position position)
        => current.ContainsKey(uri) && current[uri].Any(d => d.Range.Contains(position));

    public void Track(DocumentUri uri)
        => current.Add(uri, new List<Diagnostic>());

    public void Untrack(DocumentUri uri)
        => current.Remove(uri);

    public void Update(DocumentUri uri, CompilationState state, string source)
    {
        if (!current.ContainsKey(uri))
            return;

        supported ??= facade.ClientSettings.Capabilities?.TextDocument?.PublishDiagnostics.IsSupported ?? false;

        if (!supported.Value)
            return;

        var lines = source.Split("\n");
        var row = lines.Length - 1;
        var col = lines[^1].Length-1;
        var endOfFile = new Range(row, col, row, col);

        var warnings = state.warnings.Select(w => new Diagnostic
        {
            Severity = DiagnosticSeverity.Warning,
            Source = "vein",
            Range = w.Posed?.Transform?.ToRange() ?? new Range(),
            Code = new DiagnosticCode(99),
            Message = w.text
        });

        var errors = state.errors.Select(w => new Diagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Source = "vein",
            Range = w.Posed?.Transform?.ToRange() ?? new Range(),
            Code = new DiagnosticCode(56),
            Message = w.text
        });

        var all = warnings.Concat(errors).Distinct().ToList();
        current[uri] = all;

        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = all
        });
    }
}


public static class TransformEx
{
    public static Range ToRange(this Transform transform) =>
        new(transform.pos.Line, transform.pos.Column, transform.pos.Line,
            transform.pos.Column + transform.len);
}
