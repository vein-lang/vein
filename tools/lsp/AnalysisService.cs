using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.Diagnostics;
using vein;
using vein.compilation;
using vein.stl;
using vein.syntax;

public class AnalysisService(
    ILogger<AnalysisService> logger,
    BufferService documentService,
    IDiagnosticService diagnosticService)
{
    private readonly Dictionary<DocumentUri, (Task<DocumentDeclaration> task, CancellationTokenSource cts)> analyses = new();

    public void Reparse(DocumentUri key, ServerOptions options)
    {
        var cts = new CancellationTokenSource();
        lock (analyses)
        {
            if (!analyses.TryGetValue(key, out var pending))
                analyses[key] = (Task.Run(() => Analyse(options, key, cts.Token), cts.Token), cts);
            else if (pending.task.IsCompleted)
                analyses[key] = (Task.Run(() => Analyse(options, key, cts.Token), cts.Token), cts);
            else
                analyses[key] = (Task.Run(async () =>
                {
                    await Task.WhenAny(pending.task, Task.Delay(100, cts.Token));
                    if (!pending.task.IsCompleted)
                    {
                        logger.LogWarning($"Hanging analysis leaked for {key}");
                    }
                    return Analyse(options, key, cts.Token);
                }, cts.Token), cts);
        }
    }

    public DocumentDeclaration Analyse(ServerOptions options, DocumentUri key, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();



        var file = File.ReadAllText(key.Path);

        documentService.Add(key, file);


        var ast = new VeinSyntax().CompilationUnitV2.ParseVein(file);


        diagnosticService.Update(key, new CompilationState(), file);


        return ast;
    }

    public Task<DocumentDeclaration> GetAnalysisAsync(DocumentUri key)
    {
        return analyses[key].task;
    }
}
