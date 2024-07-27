using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

public class BufferService
{
    private readonly ConcurrentDictionary<DocumentUri, string> buffers = new();

    public void Add(DocumentUri key, string text) => buffers.TryAdd(key, text);

    public void Remove(DocumentUri key) => buffers.TryRemove(key, out _);

    public string GetText(DocumentUri key) => buffers[key];

    public void ApplyFullChange(DocumentUri key, string text)
    {
        var buffer = buffers[key];
        buffers.TryUpdate(key, text, buffer);
    }

    public void ApplyIncrementalChange(DocumentUri key, Range range, string text)
    {
        var buffer = buffers[key];
        var newText = Splice(buffer, range, text);
        buffers.TryUpdate(key, newText, buffer);
    }

    private static int GetIndex(string buffer, Position position)
    {
        var index = 0;
        for (var i = 0; i < position.Line; i++)
        {
            index = buffer.IndexOf('\n', index) + 1;
        }
        return index + position.Character;
    }

    private static string Splice(string buffer, Range range, string text)
    {
        var start = GetIndex(buffer, range.Start);
        var end = GetIndex(buffer, range.End);
        return buffer[..start] + text + buffer[end..];
    }
}
