namespace moe.lsp
{
    using System.Threading;
    using System.Threading.Tasks;
    using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
    using OmniSharp.Extensions.LanguageServer.Protocol.Document;
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;

    internal class FoldingRangeHandler : IFoldingRangeHandler
    {
        public FoldingRangeRegistrationOptions GetRegistrationOptions() =>
            new()
            {
                DocumentSelector = DocumentSelector.ForLanguage("mana")
            };

        public Task<Container<FoldingRange>?> Handle(
            FoldingRangeRequestParam request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new Container<FoldingRange>(
                    new FoldingRange {
                        StartLine = 10,
                        EndLine = 20,
                        Kind = FoldingRangeKind.Region,
                        EndCharacter = 0,
                        StartCharacter = 0
                    }
                )
            );

        public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities) 
            => new()
        {
            DocumentSelector = DocumentSelector.ForLanguage("mana")
        };
    }
}