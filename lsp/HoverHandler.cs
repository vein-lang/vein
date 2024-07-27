using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using vein.syntax;

public class HoverHandler(IDiagnosticService diagnosticService, AnalysisService analysisService) : IHoverHandler
{
    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("vein")
    };

    public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        if (diagnosticService.HasDiagnostics(request.TextDocument.Uri, request.Position))
        {
            return null;
        }

        var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);


        foreach (var clazz in analysis.Members.OfType<ClassDeclarationSyntax>())
        {
            if (clazz.Transform.Contains(request.Position))
            {
                return new Hover
                {
                    Range = clazz.Transform.ToRange(),
                    Contents = new MarkedStringsOrMarkupContent($"class {clazz.Identifier.ExpressionString}")
                };
            }
        }

        //if (analysis.Main != null)
        //{
        //    foreach (var ctx in analysis.Main.Attributes)
        //    {
        //        var key = ctx.Syntax.Key;
        //        if (key == null)
        //        {
        //            continue;
        //        }

        //        if (ctx.Range.Contains(request.Position))
        //        {
        //            return new Hover
        //            {
        //                Range = ctx.Range,
        //                Contents = new MarkedStringsOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = api.Documentation[key.AsKey] })
        //            };
        //        }
        //    }
        //}

        return null;
    }
}


public static class TransformEx2
{
    public static bool Contains(this Transform t, Position p) => t.pos.Line == p.Line && (p.Character >= t.pos.Column || p.Character <= t.pos.Column + t.len);
}
