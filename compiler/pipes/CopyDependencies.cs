namespace vein.pipes;

using System.IO;
using System.Linq;
using cmd;
using fs;

[ExcludeFromCodeCoverage]
public class CopyDependencies : CompilerPipeline
{
    public override void Action()
    {
        if (!Target.HasChanged)
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