using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

public class CompletionHandler(ILanguageServerFacade router, TypeResolver typeResolver, TextDocumentStorage filesStorage) : ICompletionHandler
{
    private readonly ILanguageServerFacade _router = router;
    private static readonly VeinSyntax syntax = new VeinSyntax();

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        if (request.Context is { TriggerCharacter: "." })
            return await HandleDotTrigger(request, cancellationToken);
        if (request.Context is { TriggerKind: CompletionTriggerKind.Invoked })
            return await HandleSpaceTrigger(request, cancellationToken);
        return new CompletionList();
    }

    public async Task<CompletionList> HandleSpaceTrigger(CompletionParams request, CancellationToken cancellationToken)
    {
        var textDocumentUri = request.TextDocument.Uri;
        var position = request.Position;
        var currentSourceFile = new FileInfo(textDocumentUri.GetFileSystemPath());

        var documentText = await GetDocumentTextAsync(request.TextDocument, cancellationToken);

        if (string.IsNullOrEmpty(documentText))
            return new CompletionList();

        var includes = typeResolver.GetIncludes(documentText);
        var types = typeResolver.ResolveStaticClasses(includes);

        if (types.Count == 0)
            return new CompletionList();


        var completionItems = types
            .Select(completion => new CompletionItem
            {
                Label = $"{completion.Name.name}",
                Detail = $"{completion.Namespace.@namespace}::{completion.Name.name}",
                Kind = CompletionItemKind.Class
            }).ToList();

        return CompletionList.From(completionItems);
    }

    public async Task<CompletionList> HandleDotTrigger(CompletionParams request, CancellationToken cancellationToken)
    {
        var textDocumentUri = request.TextDocument.Uri;
        var position = request.Position;
        var currentSourceFile = new FileInfo(textDocumentUri.GetFileSystemPath());

        var documentText = await GetDocumentTextAsync(request.TextDocument, cancellationToken);

        if (string.IsNullOrEmpty(documentText))
            return new CompletionList();

        var includes = typeResolver.GetIncludes(documentText);
        var afterDotWord = ExtractAfterDotWord(documentText, position);

        if (string.IsNullOrEmpty(afterDotWord))
            return new CompletionList();

        var type = typeResolver.ResolveClassByName(new NameSymbol(afterDotWord), includes);

        if (type is null)
            return new CompletionList();


        var completionItems = type.Methods
            .Where(x => x.IsStatic && !x.IsPrivate)
            .Select(completion => new CompletionItem
            {
                Label = $"{completion.RawName}",
                Detail = $"```{completion.Name}",
                Documentation = new StringOrMarkupContent(new MarkupContent()
                {
                    Kind = MarkupKind.Markdown,
                    Value = $"```vein\n{completion.ToString().Replace("->", "|>")}\n```"
                }),
                Kind = CompletionItemKind.Method
            }).ToList();

        return CompletionList.From(completionItems);
    }

    private string ExtractAfterDotWord(string documentText, Position position)
    {
        var lines = documentText.Split('\n');

        if (position.Line >= lines.Length)
            return string.Empty;

        var line = lines[position.Line];

        if (position.Character > line.Length)
            position.Character = line.Length;

        var subStr = line.Substring(0, position.Character);
        var lastDotIndex = subStr.LastIndexOf('.');

        if (lastDotIndex == -1 || lastDotIndex == 0)
            return string.Empty;

        var beforeDotWord = subStr.Substring(0, lastDotIndex).Trim();
        var lastSpaceIndex = beforeDotWord.LastIndexOf(' ');

        if (lastSpaceIndex != -1)
            beforeDotWord = beforeDotWord.Substring(lastSpaceIndex + 1);

        return beforeDotWord;
    }

    private async Task<string> GetDocumentTextAsync(TextDocumentIdentifier docId, CancellationToken cancellationToken)
    {
        if (filesStorage.GetDocument(docId, out var result))
            return result;
        return String.Empty;
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
            AllCommitCharacters = new Container<string>(".", "("),
            TriggerCharacters = new Container<string>(".", "(")
        };
    }
}
