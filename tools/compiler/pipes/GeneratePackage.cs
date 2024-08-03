namespace vein.pipes;

using System.Diagnostics;
using System.IO;
using System.Linq;
using cmd;
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
        var readme1 = Project.WorkDir.File("readme.md");
        var readme2 = Project.WorkDir.File("readme");
        var cert = Project.WorkDir.File("sign.cert");

        
        if (output.Exists)
            output.Delete();

        if (Project.IsWorkload)
        {
            shard
                .Storage()
                .Files(Artifacts.Concat(this.Target.Artifacts).Where(x => x.Kind == ArtifactKind.RESOURCES).Select(x => x.Path).ToArray())
                .File(icon, icon.Exists)
                .File(readme1, readme1.Exists)
                .File(readme2, readme2.Exists)
                .File(cert, cert.Exists)
                .Return()
                .Save(output);
            return;
        }

        shard
            .Storage()
            .Folder("lib", x => x
                .Files(Artifacts.Where(x => x.Kind == ArtifactKind.BINARY).Select(x => x.Path).ToArray()))
            .File(icon, icon.Exists)
            .File(readme1, readme1.Exists)
            .File(readme2, readme2.Exists)
            .File(cert, cert.Exists)
            .Return()
            .Save(output);
    }

    public override bool CanApply(CompileSettings flags)
        => flags.GeneratePackageOutput || Project.Packable;

    public override int Order => 999;
}


public static class VeinProjectExtensions
{
    public static PackageManifest GenerateManifest(this VeinProject project)
    {
        var manifest = new PackageManifest
        {
            Name = project.Name,
            Description = project.Description,
            Urls = project.Urls,
            Version = project.Version
        };
        if (project.WorkDir.File("icon.png").Exists)
        {
            manifest.Icon = "icon.png";
            manifest.HasEmbeddedIcon = true;
        }
        if (project.WorkDir.File("readme").Exists || project.WorkDir.File("readme.md").Exists)
            manifest.HasEmbbededReadme = true;

        manifest.IsPreview = manifest.Version.IsPrerelease;
        manifest.Authors = project.Authors;
        manifest.License = project.License ?? "unlicensed";
        manifest.Dependencies = project.Dependencies.Packages.ToList();
        manifest.IsWorkload = project.IsWorkload;

        foreach (var projectReference in project.Dependencies.Projects)
        {
            var proj = projectReference.ReadProject(project);
            if (proj.Packable)
                manifest.Dependencies.Add(new PackageReference(proj.Name, proj.Version));
        }


        var file = project.CacheDir.Ensure().File("manifest.json");

        if (file.Exists)
            file.Delete();
        File.WriteAllText(file.FullName, JsonConvert.SerializeObject(manifest, Formatting.Indented));

        return manifest;
    }
}
