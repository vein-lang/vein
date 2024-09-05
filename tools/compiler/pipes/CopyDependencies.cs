namespace vein.pipes;

using System.Text;
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
            if (dependency.Kind is not ArtifactKind.BINARY)
                continue;
            var t = Path.Combine(OutputDirectory.FullName, Path.GetFileName(dependency.Path.FullName));
            if (File.Exists(t)) File.Delete(t);
            File.Copy(dependency.Path.FullName, t);
        }
    }
    public override bool CanApply(CompileSettings flags) => true;
    public override int Order => 0;
}


[ExcludeFromCodeCoverage]
public class GenerateDependencyLinks : CompilerPipeline
{
    public override void Action()
    {
        if (!Target.HasChanged)
            return;
        if (Target.Project.Dependencies.Packages.Count == 0)
            return;

        var storage = new ShardStorage();
        var content = new StringBuilder();

        foreach (var package in Target.Project.Dependencies.Packages)
            content.AppendLine($"{storage.GetPackageSpace(package.Name, package.Version).SubDirectory("lib").FullName}");

        OutputDirectory.File("dependency.links").WriteAllText(content.ToString());
    }
    public override bool CanApply(CompileSettings flags) => true;
    public override int Order => 15;
}
