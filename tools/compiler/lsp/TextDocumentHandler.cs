using lsp;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using vein.stl;
using vein.syntax;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

public class TextDocumentHandler(ILanguageServerFacade languageServer, TextDocumentStorage storage, ILogger<TextDocumentHandler> logger) : TextDocumentSyncHandlerBase
{
    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "vein");

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"add file '{request.TextDocument.Uri}' into storage, success: {storage.AddDocument(request.TextDocument)}");
        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (!request.ContentChanges.Any())
            return Unit.Value;
        var text = request.ContentChanges.First().Text;
        logger.LogInformation($"update file '{request.TextDocument.Uri}' into storage, success: {storage.UpdateDocument(request.TextDocument, text)}");
        PublishDiagnostic(text, request.TextDocument.Uri);


        return Unit.Value;
    }

    private void PublishDiagnostic(string result, DocumentUri doc)
    {
        try
        {
            new VeinSyntax().CompilationUnitV2.ParseVein(result);
            languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Uri = doc,
                Diagnostics = new Container<Diagnostic>()
            });
        }
        catch (VeinParseException e)
        {
            languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Uri = doc,
                Diagnostics = new Container<Diagnostic>(new Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Code = "VEIN-0",
                    Message = $"{e.ErrorMessage} {e}",
                    Range = e.AstItem?.Transform.ToRange() ?? new Range(e.Position.Line, e.Position.Pos, e.Position.Line, e.Position.Pos + 5)
                })
            });
        }
        catch (Exception e)
        {
            languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams()
            {
                Uri = doc,
                Diagnostics = new Container<Diagnostic>(new Diagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Code = "VEIN-0",
                    Message = $"{e}",
                    Range = new Range(0, 0, result.Count(x => x == '\n'), result.Length - 1)
                })
            });
        }
    }

    public override async Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"update and save file '{request.TextDocument.Uri}' into storage, success: {storage.UpdateDocument(request.TextDocument, request.Text!)}");
        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        storage.RemoveDocument(request.TextDocument);
        return Unit.Value;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
        Change = TextDocumentSyncKind.Full,
        Save = new SaveOptions { IncludeText = true }
    };
}
