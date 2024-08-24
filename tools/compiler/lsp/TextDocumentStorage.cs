using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

public class TextDocumentStorage
{
    private readonly ConcurrentDictionary<DocumentUri, string> concurrentContent = new();

    public bool AddDocument(TextDocumentItem item)
        => concurrentContent.TryAdd(item.Uri, item.Text);

    public bool GetDocument(TextDocumentIdentifier item, out string result)
        => concurrentContent.TryGetValue(item.Uri, out result);

    public bool UpdateDocument(TextDocumentIdentifier fileId, string content)
    {
        return concurrentContent.Remove(fileId.Uri, out _) &&
        concurrentContent.TryAdd(fileId.Uri, content);
    }

    public bool RemoveDocument(TextDocumentIdentifier fileId) => concurrentContent.TryRemove(fileId.Uri, out _);
}
