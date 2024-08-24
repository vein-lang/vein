namespace lsp;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sprache;
using vein.syntax;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

public static class RangeEx
{
    public static Range ToRange(this Transform t) => new(new Position(t.pos.Line, t.pos.Pos), new Position(t.pos.Line, t.pos.Pos + t.len));

    public static void Colorize<T>(this SemanticTokensBuilder builder, T? t, SemanticTokenType token) where T : BaseSyntax, IPositionAware<T>
    {
        if (t is null)
            return;
        if (t.Transform is null)
            return;
        var tv = t.Transform;
        builder.Push(tv.pos.Line, tv.pos.Pos, tv.len, token, SemanticTokenModifier.Defaults);
    }

    public static void Colorize<T>(this SemanticTokensBuilder builder, List<T> t, SemanticTokenType token) where T : BaseSyntax, IPositionAware<T>
    {
        if (!t.Any())
            return;
        foreach (var syntax in t) builder.Colorize(syntax, token);
    }

}
