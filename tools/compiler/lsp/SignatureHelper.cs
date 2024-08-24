using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using vein.runtime;

public class SignatureHelper(TextDocumentStorage storage, TypeResolver resolver, ILogger<SignatureHelper> logger) : SignatureHelpHandlerBase
{
    protected override SignatureHelpRegistrationOptions CreateRegistrationOptions(SignatureHelpCapability capability,
        ClientCapabilities clientCapabilities) =>
        new SignatureHelpRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
            TriggerCharacters = new Container<string>("("),
            RetriggerCharacters = new Container<string>(",")
        };

    public override async Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        if (!storage.GetDocument(request.TextDocument, out var documentText))
            return new SignatureHelp();
        var position = request.Position;

        var methodSignatures = GetMethodSignatureAtPosition(documentText, position);

        if (methodSignatures is null)
            return new SignatureHelp();

        var (signatures, name) = methodSignatures.Value;
        logger.LogInformation($"Founded signature for {name}");

        var activeP = GetActiveParameterIndex(documentText, position);
        var signatureInformation = signatures.Select(x => new SignatureInformation
        {
            Label = x.Name,
            Parameters = x.Signature.Arguments.Select(p => new ParameterInformation
            {
                Label = p.Name,
                Documentation = p.ToShortTemplateString()
            }).ToArray(),
            ActiveParameter = activeP
        }).ToList();
        var signatureHelp = new SignatureHelp
        {
            Signatures = new Container<SignatureInformation>(signatureInformation),
            ActiveSignature = signatures.Count - 1,
            ActiveParameter = activeP
        };

        return signatureHelp;
    }

    public int GetActiveParameterIndex(string documentText, Position position)
    {
        var textBeforeCursor = documentText.Substring(0, GetIndexFromPosition(documentText, position));
        var lastOpenParenIndex = textBeforeCursor.LastIndexOf('(');
        if (lastOpenParenIndex == -1)
            return 0;
        var parametersText = textBeforeCursor.Substring(lastOpenParenIndex + 1);
        var commaCount = parametersText.Count(c => c == ',');
        return commaCount;
    }

    public (List<VeinMethod>, NameSymbol)? GetMethodSignatureAtPosition(string documentText, Position position)
    {
        var textBeforeCursor = documentText.Substring(0, GetIndexFromPosition(documentText, position));

        var methodCallRegex = new Regex(@"(\w+)\.(\w+)\s*\($", RegexOptions.RightToLeft);
        var match = methodCallRegex.Match(textBeforeCursor);

        if (!match.Success)
        {
            var staticMethodCallRegex = new Regex(@"(\w+)\s*\($", RegexOptions.RightToLeft);
            match = staticMethodCallRegex.Match(textBeforeCursor);

            if (!match.Success)
                return null;
        }

        var objectName = match.Groups.Count > 2 ? match.Groups[1].Value : null;
        var methodName = match.Groups.Count > 1 ? match.Groups[^1].Value : match.Groups[1].Value;

        if (!string.IsNullOrEmpty(objectName) && !string.IsNullOrEmpty(methodName))
        {
            var type = resolver.ResolveClassByName(new NameSymbol(objectName), resolver.GetIncludes(documentText));

            if (type is null)
                return null;

            var methods = type.Methods
                .Where(x => x.RawName.Equals(methodName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            return (methods, new NameSymbol(methodName));
        }

        return null;
    }

    private int GetIndexFromPosition(string documentText, Position position)
    {
        var lines = documentText.Split('\n');
        var index = 0;

        for (var i = 0; i < position.Line; i++) index += lines[i].Length + 1;

        return index + position.Character;
    }

}
