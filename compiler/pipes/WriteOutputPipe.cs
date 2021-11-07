namespace vein.pipes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using cmd;
    using fs;
    using compilation;
    using ishtar.emit;
    using MoreLinq;
    using project;

    public class WriteOutputPipe : CompilerPipeline
    {
        public override void Action()
        {
            if (!OutputDirectory.Exists)
                OutputDirectory.Create();
            else if(Target.HasChanged)
                OutputDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).ForEach(x => x.Delete());


            var wil_file = new FileInfo(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wll.bin"));


            if (Target.HasChanged)
            {
                var wil_data = Module.BakeByteArray();


                Assembly = new IshtarAssembly(Module);

                IshtarAssembly.WriteTo(Assembly, OutputBinaryPath.FullName);

                File.WriteAllBytes(wil_file.FullName, wil_data);
            }
           
            PopulateArtifact(new ILArtifact(wil_file, Project));
            PopulateArtifact(new BinaryArtifact(OutputBinaryPath, Project));
            PopulateArtifact(new DebugSymbolArtifact(new FileInfo($"{wil_file.FullName}.lay"), Project));
        }

        public override bool CanApply(CompileSettings flags) => true;
        public override int Order => 0;
    }

    public class CopyDependencies : CompilerPipeline
    {
        public override void Action()
        {
            if(!Target.HasChanged)
                return;

            foreach (var dependency in Target.Dependencies.SelectMany(x => x.Artifacts))
            {
                if (dependency.Kind is ArtifactKind.BINARY)
                    File.Copy(dependency.Path.FullName,
                        Path.Combine(OutputDirectory.FullName, Path.GetFileName(dependency.Path.FullName)));
            }
        }
        public override bool CanApply(CompileSettings flags) => true;
        public override int Order => 0;
    }

    public abstract class CompilerPipeline
    {
        protected DirectoryInfo OutputDirectory
            => new(Path.Combine(Project.WorkDir.FullName, "bin"));
        protected FileInfo OutputBinaryPath =>
            new(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wll"));

        protected internal VeinModuleBuilder Module { get; set; }
        protected internal VeinProject Project { get; set; }
        protected internal IshtarAssembly Assembly { get; set; }
        protected internal CompilationTarget Target { get; set; }

        public abstract void Action();

        public Action<VeinArtifact> PopulateArtifact;

        public abstract bool CanApply(CompileSettings flags);
        public abstract int Order { get; }
    }

    public class PipelineRunner
    {
        public static List<CompilerPipeline> GetPipes() =>
        new()
        {
            new WriteOutputPipe(),
            new SingleFileOutputPipe(),
            new CopySDKBinaries(),
            new CopyDependencies()
        };


        public static void Run(CompilationTask compiler, CompilationTarget target)
        {
            var lastPipe = default(CompilerPipeline);

            var pipes = GetPipes();
            var task = compiler.StatusCtx.AddTask("Running post-compile task...", maxValue: pipes.Count);

            foreach (var pipe in pipes)
            {
                if (!pipe.CanApply(compiler._flags))
                {
                    task.Increment(1);
                    continue;
                }
                task.VeinStatus($"Apply [orange]'{pipe.GetType().Name}'[/] pipeline...");
                pipe.PopulateArtifact = (x) => compiler.artifacts.Add(x);
                pipe.Project = lastPipe?.Project ?? compiler.Project;
                pipe.Assembly = lastPipe?.Assembly;
                pipe.Module = lastPipe?.Module ?? compiler.module;
                pipe.Target = target;

                pipe.Action();
                Thread.Sleep(400);

                lastPipe = pipe;
                task.Increment(1);
            }
            task.StopTask();
        }
    }
}
