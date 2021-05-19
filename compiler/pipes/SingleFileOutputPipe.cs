namespace wave.pipes
{
    using System;
    using System.IO;
    using System.Linq;
    using cmd;
    using project;

    public class SingleFileOutputPipe : CompilerPipeline
    {
        public override void Action()
        {
            var current_binary = OutputBinaryPath.ReadAllBytes();
            var host = Project.SDK.
                GetHostApplicationFile(Project.SDK.
                    GetPackByAlias(Project.Runtime));

            var host_bytes = host.ReadAllBytes().ToList();
            var offset = host_bytes.Count;

            host_bytes.AddRange(current_binary);
            
            host_bytes.AddRange(BitConverter.GetBytes(offset));
            // magic number (for detect single file exe)
            host_bytes.AddRange(BitConverter.GetBytes((short)0x7ABC));

            var binary_name = $"{Project.Name}{Path.GetExtension(host.FullName)}";

            File.WriteAllBytes(Path.Combine(OutputDirectory.FullName, binary_name), host_bytes.ToArray());
        }

        public override bool CanApply(CompileSettings flags) => flags.HasSingleFile;

        public override int Order => 999;
    }
}