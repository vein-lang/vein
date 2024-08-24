using ishtar;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

public class LSPAssemblyResolver(ILanguageServerFacade languageServer) : ModuleResolverBase
{
    protected override void debug(string s) => languageServer.Window.ShowInfo(s);
}
