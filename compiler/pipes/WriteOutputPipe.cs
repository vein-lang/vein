namespace mana.pipes
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using cmd;
    using fs;
    using insomnia.compilation;
    using ishtar.emit;
    using MoreLinq;
    using project;

    public class WriteOutputPipe : CompilerPipeline
    {
        public override void Action()
        {
            if (!OutputDirectory.Exists)
                OutputDirectory.Create();
            else
                OutputDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).ForEach(x => x.Delete());


            var wil_file = new FileInfo(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wvil.bin"));

            var wil_data = Module.BakeByteArray();


            Assembly = new IshtarAssembly(Module);

            IshtarAssembly.WriteTo(Assembly, OutputBinaryPath.FullName);

            File.WriteAllBytes(wil_file.FullName, wil_data);
        }

        public override bool CanApply(CompileSettings flags) => true;
        public override int Order => 0;
    }


    public abstract class CompilerPipeline
    {
        protected DirectoryInfo OutputDirectory
            => new(Path.Combine(Project.WorkDir, "bin"));
        protected FileInfo OutputBinaryPath =>
            new FileInfo(Path.Combine(OutputDirectory.FullName, $"{Project.Name}.wll"));

        protected internal ManaModuleBuilder Module { get; set; }
        protected internal ManaProject Project { get; set; }
        protected internal IshtarAssembly Assembly { get; set; }

        public abstract void Action();



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
            new CopySDKBinaries()
        };


        public static void Run(Compiler compiler)
        {
            var lastPipe = default(CompilerPipeline);
            foreach (var pipe in GetPipes())
            {
                if (!pipe.CanApply(compiler._flags))
                    continue;
                compiler.Status.ManaStatus($"Apply '{pipe.GetType().Name}' pipeline...");
                pipe.Project = lastPipe?.Project ?? compiler.Project;
                pipe.Assembly = lastPipe?.Assembly;
                pipe.Module = lastPipe?.Module ?? compiler.module;

                pipe.Action();
                Thread.Sleep(400);

                lastPipe = pipe;
            }
        }
    }
}
