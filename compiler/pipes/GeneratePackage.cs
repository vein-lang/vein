namespace vein.pipes;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using cmd;
using compilation;
using fs;
using Newtonsoft.Json;
using project;
using project.shards;

[ExcludeFromCodeCoverage]
public class GeneratePackage : CompilerPipeline
{
    public override void Action()
    {
        var manifest = Project.GenerateManifest();

        var shard = new ShardBuilder(manifest, (x) => Log.Info(x, Target));
        var output = OutputDirectory.File($"{Project.Name}-{Project.Version}.shard");
        var icon = Project.WorkDir.File("icon.png");
        var cert = Project.WorkDir.File("sign.cert");


        if (output.Exists)
            output.Delete();

        shard
            .Storage()
            .Folder("lib", x => x
                .Files(Artifacts.Where(x => x.Kind == ArtifactKind.BINARY).Select(x => x.Path).ToArray()))
            .File(icon, icon.Exists)
            .File(cert, cert.Exists)
            .Return()
            .Save(output);
    }

    public override bool CanApply(CompileSettings flags)
        => flags.GeneratePackageOutput || Project.Packable;

    public override int Order { get; } = 999;
}


public static class VeinProjectExtensions
{
    public static PackageManifest GenerateManifest(this VeinProject project)
    {
        var manifest = new PackageManifest();
        manifest.Name = project.Name;
        manifest.Description = "";
        manifest.Version = project.Version;
        if (project.WorkDir.File("icon.png").Exists)
            manifest.Icon = "icon.png";
        manifest.IsPreview = manifest.Version.IsPrerelease;
        manifest.Authors = new List<string>() { project.Author };
        manifest.License = project.License ?? "unlicensed";
        manifest.Dependencies = project.Dependencies.Packages.ToList();


        var file = project.CacheDir.File("manifest.json");

        if (file.Exists)
            file.Delete();
        File.WriteAllText(file.FullName, JsonConvert.SerializeObject(manifest, Formatting.Indented));

        return manifest;
    }
}
