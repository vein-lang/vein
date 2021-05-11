namespace moe.lsp
{
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using OmniSharp.Extensions.LanguageServer.Protocol;
    using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
    using OmniSharp.Extensions.LanguageServer.Protocol.Document;
    using OmniSharp.Extensions.LanguageServer.Protocol.Models;
    using wave.stl;
    using wave.syntax;

    public class HoverHandler : HoverHandlerBase
    {
        private static WaveSyntax _syntax = new();

        #region Overrides of Base<HoverRegistrationOptions,HoverCapability>

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
            => new HoverRegistrationOptions();

        #endregion

        #region Overrides of Request<HoverParams,Hover?,HoverRegistrationOptions,HoverCapability>

        public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request.TextDocument.Uri), cancellationToken);
            await Task.Yield();
            var result = _syntax.CompilationUnit.ParseWave(content);

            var lines = content.Split('\n');

            var line = lines[request.Position.Line];

            if (!char.IsLetter(line[request.Position.Character]))
                return new Hover();
            var word = GetWordByCharIndex(line, request.Position.Character);


            return null;
        }


        public static string GetWordByCharIndex(string line, int charIndex)
        {
            var first = "";
            var firstCollection = line.Take(charIndex).ToArray();

            for (var i = firstCollection.Length - 1; i != 0 && firstCollection[i] != ' '; i--)
                first += firstCollection[i];

            var last = "";
            var lastCollection = line.Skip(charIndex).ToArray();

            for (var i = 0; i != lastCollection.Length && lastCollection[i] != ' '; i++)
                last += lastCollection[i];

            return string.Join("", first.Reverse()) + string.Join("", last.Reverse());
        }

        #endregion
    }
}