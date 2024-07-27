using System.Net.Sockets;
using System.Net;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;

var pipeName = "vein_language_pipe";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .MinimumLevel.Verbose()
    .CreateLogger();

//await using var pipeServer =
//    new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message);
//Console.WriteLine($"LSP Server started on pipe://{pipeName}");

//var pipeReader = PipeReader.Create(pipeServer);
//var pipeWriter = PipeWriter.Create(pipeServer);

//await pipeServer.WaitForConnectionAsync();
//var server = new LanguageServer(pipeWriter.AsStream(), pipeReader.AsStream());
//await server.StartAsync();

const int port = 7777;
var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
Console.WriteLine($"LSP Server started on tcp://localhost:{port}");
var client = await listener.AcceptTcpClientAsync();
Console.WriteLine("Client connected!");




var server = await LanguageServer.From(options =>
{
    options
        .WithInput(client.GetStream())
        .WithOutput(client.GetStream())
        .ConfigureLogging(
            x => x
                .AddSerilog(Log.Logger)
                .SetMinimumLevel(LogLevel.Debug)
        )
        .OnInitialize(async (server, request, token) =>
        {
            Console.WriteLine("Initialize request received");
            await Task.Delay(1);
        })
        .OnInitialized(async (languageServer, request, response, token) =>
        {
            Console.WriteLine("Initialized");
            await Task.Delay(1);
        })
        .OnStarted(
            async (languageServer, token) =>
            {
                //using var manager = await languageServer.WorkDoneManager.Create(new WorkDoneProgressBegin { Title = "Doing some work..." })
                //    .ConfigureAwait(true);

                //manager.OnNext(new WorkDoneProgressReport { Message = "doing things..." });
                //await Task.Delay(10000).ConfigureAwait(true);
                //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 1234" });
                //await Task.Delay(10000).ConfigureAwait(true);
                //manager.OnNext(new WorkDoneProgressReport { Message = "doing things... 56789" });

                var logger = languageServer.Services.GetService<ILogger<LanguageServer>>();
                var configuration = await languageServer.Configuration.GetConfiguration(
                    new ConfigurationItem
                    {
                        Section = "vein",
                    }
                ).ConfigureAwait(false);

                var baseConfig = new JObject();
                foreach (var config in languageServer.Configuration.AsEnumerable())
                {
                    baseConfig.Add(config.Key, config.Value);
                }

                logger.LogInformation("Base Config: {@Config}", baseConfig);

                var scopedConfig = new JObject();
                foreach (var config in configuration.AsEnumerable())
                {
                    scopedConfig.Add(config.Key, config.Value);
                }

                logger.LogInformation("Scoped Config: {@Config}", scopedConfig);
            }
        )
        .WithHandler<HoverHandler>()
        .WithHandler<TextDocumentSyncHandler>()
        .WithHandler<VeinBuildCommandHandler>()
        .WithServices(ServicesAction);

    void ServicesAction(IServiceCollection obj)
    {
        obj.AddSingleton<IDiagnosticService, DiagnosticService>();
        obj.AddSingleton<BufferService>();
    }
});

await server.WaitForExit;

//internal class MyDocumentSymbolHandler : DocumentSymbolHandlerBase
//{
//    public override async Task<SymbolInformationOrDocumentSymbolContainer> Handle(
//        DocumentSymbolParams request,
//        CancellationToken cancellationToken
//    )
//    {
//        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
//        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(request), cancellationToken).ConfigureAwait(false);
//        var lines = content.Split('\n');
//        var symbols = new List<SymbolInformationOrDocumentSymbol>();
//        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
//        {
//            var line = lines[lineIndex];
//            var parts = line.Split(' ', '.', '(', ')', '{', '}', '[', ']', ';');
//            var currentCharacter = 0;
//            foreach (var part in parts)
//            {
//                if (string.IsNullOrWhiteSpace(part))
//                {
//                    currentCharacter += part.Length + 1;
//                    continue;
//                }

//                symbols.Add(
//                    new DocumentSymbol
//                    {
//                        Detail = part,
//                        Deprecated = true,
//                        Kind = SymbolKind.Field,
//                        Tags = new[] { SymbolTag.Deprecated },
//                        Range = new Range(
//                            new Position(lineIndex, currentCharacter),
//                            new Position(lineIndex, currentCharacter + part.Length)
//                        ),
//                        SelectionRange =
//                            new Range(
//                                new Position(lineIndex, currentCharacter),
//                                new Position(lineIndex, currentCharacter + part.Length)
//                            ),
//                        Name = part
//                    }
//                );
//                currentCharacter += part.Length + 1;
//            }
//        }

//        // await Task.Delay(2000, cancellationToken);
//        return symbols;
//    }

//    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability,
//        ClientCapabilities clientCapabilities) => new()
//    {
//        DocumentSelector = TextDocumentSelector.ForLanguage("vein"),
//        Label = "yes"
//    };

//}
