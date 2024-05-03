namespace ishtar;

using Iced.Intel;

internal sealed class AssemblerFormatterOutputImpl : FormatterOutput
{
    public readonly List<(string text, FormatterTextKind kind)> List =
        new List<(string text, FormatterTextKind kind)>();
    public override void Write(string text, FormatterTextKind kind) => List.Add((text, kind));
}