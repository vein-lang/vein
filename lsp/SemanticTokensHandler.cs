namespace moe.lsp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using OmniSharp.Extensions.LanguageServer.Protocol;
    using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
    using OmniSharp.Extensions.LanguageServer.Protocol.Document;
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;
    using Sprache;
    using mana.stl;
    using mana.syntax;

    public class SemanticTokensHandler : SemanticTokensHandlerBase
    {
        private readonly ILogger _logger;
        private readonly ManaSyntax _syntax = new();

        public SemanticTokensHandler(ILogger<SemanticTokensHandler> logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public override async Task<SemanticTokens?> Handle(
            SemanticTokensParams request, CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken);
            return result;
        }

        public override async Task<SemanticTokens?> Handle(
            SemanticTokensRangeParams request, CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken);
            return result;
        }

        public override async Task<SemanticTokensFullOrDelta?> Handle(
            SemanticTokensDeltaParams request,
            CancellationToken cancellationToken
        )
        {
            var result = await base.Handle(request, cancellationToken);
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
            var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(identifier), cancellationToken);
            await Task.Yield();


            var result = _syntax.CompilationUnit.End().ParseMana(content);


            foreach (var directive in result.Directives)
            {
                var transform = directive.Transform;
                builder.Push(transform.pos.Line, transform.pos.Pos, transform.len, SemanticTokenType.Macro, 
                    new List<SemanticTokenModifier>());
            }

            foreach (var member in result.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    var transform = clazz.Identifier.Transform;
                    builder.Push(transform.pos.Line, transform.pos.Pos, transform.len, SemanticTokenType.Class, 
                        SemanticTokenModifier.Static);

                    foreach (var method in clazz.Methods)
                    {

                        var transform2 = method.Identifier.Transform;
                        builder.Push(transform2.pos.Line, transform2.pos.Pos, transform2.len, 
                            SemanticTokenType.Class, 
                            SemanticTokenModifier.Static);
                    }
                }
                
                
            }

            //foreach (var (line, text) in content.Split('\n').Select((text, line) => (line, text)))
            //{
                
            //    var parts = text.TrimEnd().Split(';', ' ', '.', '"', '(', ')');
            //    var index = 0;
            //    foreach (var part in parts)
            //    {
            //        typesEnumerator.MoveNext();
            //        modifiersEnumerator.MoveNext();
            //        if (string.IsNullOrWhiteSpace(part)) continue;
            //        index = text.IndexOf(part, index, StringComparison.Ordinal);
            //        builder.Push(line, index, part.Length, typesEnumerator.Current, modifiersEnumerator.Current);
            //    }
            //}
        }

        protected override Task<SemanticTokensDocument>
            GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) =>
            Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));


        private IEnumerable<T> RotateEnum<T>(IEnumerable<T> values)
        {
            while (true)
            {
                foreach (var item in values)
                    yield return item;
            }
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) => new SemanticTokensRegistrationOptions {
            DocumentSelector = DocumentSelector.ForLanguage("mana"),
            Legend = new SemanticTokensLegend() {
                TokenModifiers = capability.TokenModifiers,
                TokenTypes = capability.TokenTypes
            },
            Full = new SemanticTokensCapabilityRequestFull {
                Delta = true
            },
            Range = true
        };
    }
}