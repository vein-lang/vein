namespace vein.pipes;

using System.Collections.Generic;
using System.Threading;
using compilation;
using fs;

[ExcludeFromCodeCoverage]
public class PipelineRunner
{
    public static List<CompilerPipeline> GetPipes() => new()
    {
        new WriteOutputPipe(),
        new SingleFileOutputPipe(),
        new CopySDKBinaries(),
        new CopyDependencies(),
        new GeneratePackage()
    };


    public static void Run(CompilationTask compiler, CompilationTarget target)
    {
        var lastPipe = default(CompilerPipeline);

        var pipes = GetPipes();
        var task = compiler.StatusCtx.AddTask("Running post-compile task...", maxValue: pipes.Count);

        foreach (var pipe in pipes)
        {
            pipe.Project = lastPipe?.Project ?? compiler.Project;
            pipe.PopulateArtifact = (x) => compiler.artifacts.Add(x);
            pipe.Assembly = lastPipe?.Assembly;
            pipe.Module = lastPipe?.Module ?? compiler.module;
            pipe.Target = target;
            pipe.Artifacts = lastPipe?.Artifacts ?? new List<VeinArtifact>();

            if (!pipe.CanApply(compiler._flags))
            {
                task.Increment(1);
                continue;
            }
            task.VeinStatus($"Apply [orange]'{pipe.GetType().Name}'[/] pipeline...");
            pipe.Action();
            Thread.Sleep(400);

            lastPipe = pipe;
            task.Increment(1);
        }
        task.StopTask();
    }
}
