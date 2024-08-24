namespace lsp;

using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using vein;
using vein.syntax;
using Log = Serilog.Log;

public class LspSettings : CommandSettings
{
    [Description("Tcp Port")]
    [CommandOption("--port")]
    public int TcpPort { get; set; }
}

public class LspRunCommand : AsyncCommand<LspSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LspSettings settings)
    {
        var listener = new TcpListener(IPAddress.Loopback, settings.TcpPort);
        listener.Start();
        Log.Information(($"LSP Server started on tcp://localhost:{settings.TcpPort}"));
        var client = await listener.AcceptTcpClientAsync();
        Log.Information("Client connected!");

        var server = await LanguageServer.From(options =>
        {
            options
                .WithServices(ServicesAction)
                .WithInput(client.GetStream())
                .WithOutput(client.GetStream())

                .OnInitialized((languageServer, request, response, token) =>
                {
                    var workspace = languageServer.GetRequiredService<WorkspaceService>();

                    return workspace.Begin(request, token);
                })
                .ConfigureLogging(
                    x => x
                        .AddSerilog(Log.Logger)
                        .SetMinimumLevel(LogLevel.Debug)
                )
                .WithHandler<CompletionHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<TextDocumentHandler>()
                .WithHandler<SignatureHelper>();

            void ServicesAction(IServiceCollection obj)
            {
                obj.AddSingleton<WorkspaceService>();
                obj.AddSingleton<ShardStorage>();
                obj.AddSingleton<LSPAssemblyResolver>();
                obj.AddSingleton<VeinSyntax>();
                obj.AddSingleton<TypeResolver>();
                obj.AddSingleton<TextDocumentStorage>();
            }
        });

        await server.WaitForExit;

        return 0;
    }
}
