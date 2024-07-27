using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

internal class TextDocumentSyncHandler(
    ILogger<TextDocumentSyncHandler> logger,
    ILanguageServerConfiguration configuration,
    IDiagnosticService diagnosticService)
    : TextDocumentSyncHandlerBase
{
    private readonly BufferService documentService;
    private readonly AnalysisService semanticService;


    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
        Change = TextDocumentSyncKind.Incremental,
        Save = new SaveOptions { IncludeText = false }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "vein");

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var config = await configuration.GetScopedConfiguration(request.TextDocument.Uri, cancellationToken);
        var options = new ServerOptions();
        config.GetSection("vein").GetSection("server").Bind(options);

        documentService.Add(request.TextDocument.Uri, request.TextDocument.Text);
        diagnosticService.Track(request.TextDocument.Uri);
        semanticService.Reparse(request.TextDocument.Uri, options);

        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        if (configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }

        diagnosticService.Untrack(request.TextDocument.Uri);
        documentService.Remove(request.TextDocument.Uri);

        return Unit.Task;
    }


    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var config = await configuration.GetScopedConfiguration(request.TextDocument.Uri, cancellationToken);
        var options = new ServerOptions();
        config.GetSection("vein").GetSection("server").Bind(options);

        foreach (var change in request.ContentChanges)
        {
            if (change.Range != null)
                documentService.ApplyIncrementalChange(request.TextDocument.Uri, change.Range, change.Text);
            else
                documentService.ApplyFullChange(request.TextDocument.Uri, change.Text);
        }

        semanticService.Reparse(request.TextDocument.Uri, options);

        return Unit.Value;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) => Unit.Task;
}
