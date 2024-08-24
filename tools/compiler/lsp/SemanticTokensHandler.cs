using System.Linq.Expressions;
using lsp;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using vein.stl;
using vein.syntax;

public class SemanticTokensHandler(TextDocumentStorage filesStorage, VeinSyntax syntax, ILogger<SemanticTokensHandler> logger) : SemanticTokensHandlerBase
{
    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
        Full = true,
        Legend = new SemanticTokensLegend
        {
            TokenModifiers = capability.TokenModifiers,
            TokenTypes = capability.TokenTypes
        },
        Range = true
    };


    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        if (!filesStorage.GetDocument(identifier.TextDocument, out var result))
            return Task.CompletedTask;
        try
        {
            var ast = syntax.CompilationUnitV2.ParseVein(result);

            foreach (var alias in ast.Aliases)
            {
                builder.Colorize(alias.Type, SemanticTokenType.Type);
                builder.Colorize(alias.AliasName, SemanticTokenType.Keyword);
                builder.Colorize(alias.Generics, SemanticTokenType.TypeParameter);
                if (alias.MethodDeclaration is null)
                    continue;
                builder.Colorize(alias.MethodDeclaration.Identifier, SemanticTokenType.Keyword);
                builder.Colorize(alias.MethodDeclaration.GenericTypes, SemanticTokenType.TypeParameter);
                builder.Colorize(alias.MethodDeclaration.Parameters, SemanticTokenType.Parameter);
                builder.Colorize(alias.MethodDeclaration.ReturnType, SemanticTokenType.Type);
            }

            foreach (var member in ast.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    builder.Colorize(clazz.Identifier, SemanticTokenType.Keyword);
                    builder.Colorize(clazz.GenericTypes, SemanticTokenType.TypeParameter);
                    builder.Colorize(clazz.Inheritances, SemanticTokenType.Type);
                    builder.Colorize(clazz.TypeParameterConstraints, SemanticTokenType.Interface);


                    foreach (var method in clazz.Methods)
                    {
                        builder.Colorize(method.Identifier, SemanticTokenType.Decorator);
                        builder.Colorize(method.GenericTypes, SemanticTokenType.TypeParameter);
                        builder.Colorize(method.ReturnType, SemanticTokenType.Type);

                        if (method.Body is null)
                            continue;

                        foreach (var statement in method.Body.Statements)
                        {
                            if (statement is QualifiedExpressionStatement q)
                            {
                                if (q.Value is BinaryExpressionSyntax bin)
                                {
                                    if (bin.OperatorType == ExpressionType.MemberAccess)
                                    {
                                        builder.Colorize(bin.Left, SemanticTokenType.String);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            builder.Commit();
        }
        catch (Exception e)
        {
            logger.LogError(e, "err");
        }

        return Task.CompletedTask;
    }

    protected override async Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) =>
        new(RegistrationOptions);
}
