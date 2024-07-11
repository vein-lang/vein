using System.IO.Pipelines;
using System.IO.Pipes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Progress;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using FileSystemWatcher = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

var pipeName = "vein_language_pipe";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Verbose()
    .CreateLogger();

//await using var pipeServer =
//    new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances);
//Console.WriteLine($"LSP Server started on pipe://{pipeName}");

//await pipeServer.WaitForConnectionAsync();

//Console.WriteLine("Client connected!");

//var pipeReader = PipeReader.Create(pipeServer);
//var pipeWriter = PipeWriter.Create(pipeServer);

var server = await LanguageServer.From(options =>
{
    options
        .WithInput(Console.OpenStandardInput())
        .WithOutput(Console.OpenStandardOutput())
        .ConfigureLogging(
            x => x
                .AddSerilog(Log.Logger)
                .AddLanguageProtocolLogging()
                .SetMinimumLevel(LogLevel.Debug)
        )
        .WithHandler<HoverHandler>()
        .WithHandler<FoldingRangeHandler>()
        .WithHandler<SemanticTokensHandler>()
        .WithHandler<DidChangeWatchedFilesHandler>()
        .WithHandler<TextDocumentHandler>()
        .WithHandler<MyDocumentSymbolHandler>()
        .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)));

}).ConfigureAwait(false);




await server.WaitForExit;

class HoverHandler : HoverHandlerBase
{
    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = DocumentSelector.ForLanguage("vein")
    };

    public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        return new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkedString("xuita"))
        };
    }
}

internal class FoldingRangeHandler : FoldingRangeHandlerBase
{
    public override Task<Container<FoldingRange>?> Handle(
        FoldingRangeRequestParam request,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult<Container<FoldingRange>?>(
            new Container<FoldingRange>(
                new FoldingRange
                {
                    StartLine = 10,
                    EndLine = 20,
                    Kind = FoldingRangeKind.Region,
                    EndCharacter = 0,
                    StartCharacter = 0
                }
            )
        );

    protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(FoldingRangeCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = DocumentSelector.ForLanguage("vein"),
    };

}

public class SemanticTokensHandler(ILogger<SemanticTokensHandler> logger) : SemanticTokensHandlerBase
{
    private readonly ILogger _logger = logger;

    public override async Task<SemanticTokens?> Handle(
        SemanticTokensParams request, CancellationToken cancellationToken
    )
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokens?> Handle(
        SemanticTokensRangeParams request, CancellationToken cancellationToken
    )
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokensFullOrDelta?> Handle(
        SemanticTokensDeltaParams request,
        CancellationToken cancellationToken
    )
    {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected override async Task Tokenize(
        SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken
    )
    {
        using var typesEnumerator = RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
        using var modifiersEnumerator = RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(identifier), cancellationToken).ConfigureAwait(false);
        await Task.Yield();

        foreach (var (line, text) in content.Split('\n').Select((text, line) => (line, text)))
        {
            var parts = text.TrimEnd().Split(';', ' ', '.', '"', '(', ')');
            var index = 0;
            foreach (var part in parts)
            {
                typesEnumerator.MoveNext();
                modifiersEnumerator.MoveNext();
                if (string.IsNullOrWhiteSpace(part)) continue;
                index = text.IndexOf(part, index, StringComparison.Ordinal);
                builder.Push(line, index, part.Length, typesEnumerator.Current, modifiersEnumerator.Current);
            }
        }
    }

    protected override Task<SemanticTokensDocument>
        GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }


    private IEnumerable<T> RotateEnum<T>(IEnumerable<T> values)
    {
        while (true)
        {
            foreach (var item in values)
                yield return item;
        }
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities
    )
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("vein"),
            Legend = new SemanticTokensLegend
            {
                TokenModifiers = capability.TokenModifiers,
                TokenTypes = capability.TokenTypes
            },
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true
            },
            Range = true,
            WorkDoneProgress = true
        };
    }
}


internal class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
{
    public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions()
    {
        return new DidChangeWatchedFilesRegistrationOptions()
        {
            Watchers = new Container<FileSystemWatcher>(new FileSystemWatcher()
            {
                GlobPattern = "**/*.vein"
            })
        };
    }

    public Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken) => Unit.Task;

    public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(DidChangeWatchedFilesCapability capability,
        ClientCapabilities clientCapabilities) => GetRegistrationOptions();
}


internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<TextDocumentHandler> _logger;
    private readonly ILanguageServerConfiguration _configuration;

    private readonly DocumentSelector _textDocumentSelector = DocumentSelector.ForLanguage("vein");

    public TextDocumentHandler(ILogger<TextDocumentHandler> logger,ILanguageServerConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    public override Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
    {
        _logger.LogCritical("Critical");
        _logger.LogDebug("Debug");
        _logger.LogTrace("Trace");
        _logger.LogInformation("Hello world!");
        return Unit.Task;
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
    {
        await Task.Yield();
        _logger.LogInformation("Hello world!");
        await _configuration.GetScopedConfiguration(notification.TextDocument.Uri, token).ConfigureAwait(false);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
    {
        if (_configuration.TryGetScopedConfiguration(notification.TextDocument.Uri, out var disposable))
        {
            disposable.Dispose();
        }

        return Unit.Task;
    }
    
    public override Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token) => Unit.Task;

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = _textDocumentSelector,
        Change = Change,
        Save = new SaveOptions() { IncludeText = true }
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "vein");
}

internal class MyDocumentSymbolHandler : DocumentSymbolHandlerBase
{
    public override async Task<SymbolInformationOrDocumentSymbolContainer> Handle(
        DocumentSymbolParams request,
        CancellationToken cancellationToken
    )
    {
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request), cancellationToken).ConfigureAwait(false);
        var lines = content.Split('\n');
        var symbols = new List<SymbolInformationOrDocumentSymbol>();
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var parts = line.Split(' ', '.', '(', ')', '{', '}', '[', ']', ';');
            var currentCharacter = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    currentCharacter += part.Length + 1;
                    continue;
                }

                symbols.Add(
                    new DocumentSymbol
                    {
                        Detail = part,
                        Deprecated = true,
                        Kind = SymbolKind.Field,
                        Tags = new[] { SymbolTag.Deprecated },
                        Range = new Range(
                            new Position(lineIndex, currentCharacter),
                            new Position(lineIndex, currentCharacter + part.Length)
                        ),
                        SelectionRange =
                            new Range(
                                new Position(lineIndex, currentCharacter),
                                new Position(lineIndex, currentCharacter + part.Length)
                            ),
                        Name = part
                    }
                );
                currentCharacter += part.Length + 1;
            }
        }

        // await Task.Delay(2000, cancellationToken);
        return symbols;
    }

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = DocumentSelector.ForLanguage("vein"),
        Label = "yes"
    };

}
