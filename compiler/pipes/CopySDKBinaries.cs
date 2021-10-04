namespace vein.pipes
{
    using System.IO;
    using cmd;
    using MoreLinq;

    public class CopySDKBinaries : CompilerPipeline
    {
        public override int Order => 1;

        public override void Action()
        {
            var current_path = OutputBinaryPath.Directory;
            var files = Project.SDK.
                GetFullPath(Project.SDK.
                    GetPackByAlias(Project.Runtime)).EnumerateFiles("*.wll");

            var bin = current_path.SubDirectory("refs");

            bin.Create();

            files.ForEach(f => File.Copy(f.FullName, Path.Combine(bin.FullName, f.Name)));
        }
        public override bool CanApply(CompileSettings flags) => true;
    }
}
