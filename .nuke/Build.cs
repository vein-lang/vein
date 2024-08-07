using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Project = Nuke.Common.ProjectModel.Project;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.NSwag.NSwagTasks;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;


[GitHubActions("build_nuke", GitHubActionsImage.UbuntuLatest, AutoGenerate = false,
    On = [GitHubActionsTrigger.Push],
    ImportSecrets = ["VEIN_API_KEY", "GITHUB_TOKEN"],
    EnableGitHubToken = true)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);
    
    [GitRepository] readonly GitRepository Repository;

    GitHubActions GitHubActions => GitHubActions.Instance;

    [Solution] readonly Solution Solution;

    [GitVersion] readonly GitVersion GitVersion;
    [Description("Mark build as publish workloads")]
    [Parameter] readonly bool HasPublishWorkloads;

    AbsolutePath OutputDirectory => RootDirectory / "output";


    SolutionFolder Tools => Solution.GetSolutionFolder("tools");
    SolutionFolder Backends => Solution.GetSolutionFolder("backends");

    Project Veinc => Tools.GetProject("veinc");
    Project RuneCLI => Tools.GetProject("rune-cli");
    Project Ishtar => Backends.GetProject("ishtar.vm");

    
    [Parameter] [Secret] readonly string GithubToken;

    AbsolutePath WorkloadRuntime => RootDirectory / "workloads" / "runtime";
    AbsolutePath WorkloadCompiler => RootDirectory / "workloads" / "compiler";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            OutputDirectory.CreateOrCleanDirectory();
            DotNetClean(c => c
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .Executes(() => {
            DotNetRestore(c => c
                .SetProjectFile(Solution));
        });

    #region Rune

    Target BuildRune => _ => _
        .DependsOn(Restore)
        .Produces(OutputDirectory / "*.zip")
        .Executes(() => {
            var runtimes = new[]
            {
                "win-x64",
                "linux-x64",
                "linux-arm64",
                "osx-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime => {
                var outputDir =  OutputDirectory / $"rune" / runtime;
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(RuneCLI)
                    .SetConfiguration(Configuration.Release)
                    .EnablePublishSingleFile()
                    .EnableSelfContained()
                    .SetRuntime(runtime)
                    .SetOutput(outputDir)
                .EnableNoRestore());

                Compress(outputDir, OutputDirectory / $"rune.{runtime}.zip", x => !x.HasExtension("xml", "pdb"));
            });
        });

    #endregion


    #region Veinc

    Target BuildVeinc => _ => _
        .DependsOn(Restore)
        .Executes(() => {
            var runtimes = new[]
            {
                "win-x64",
                "linux-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime => {
                var outputDir =  OutputDirectory / $"veinc" / runtime;
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(Veinc)
                    .SetConfiguration(Configuration.Release)
                    .EnablePublishSingleFile()
                    .EnableSelfContained()
                    .SetRuntime(runtime)
                    .SetOutput(outputDir)
                    .EnableNoRestore());

                Compress(outputDir, OutputDirectory / $"veinc.compiler.{runtime}.zip");
            });
        });

    #endregion


    #region Ishtar

    Target BuildIshtarDebug => _ => _
        .DependsOn(Restore)
        .Executes(() => {
            var runtimes = new[]
            {
                "win-x64",
                "linux-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime => {
                var outputDir =  OutputDirectory / $"ishtar" / runtime / "debug";
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(Ishtar)
                    .SetConfiguration(Configuration.Debug)
                    .EnablePublishSingleFile()
                    .EnableSelfContained()
                    .SetRuntime(runtime)
                    .SetOutput(outputDir)
                    .EnableNoRestore());
            });
        });

    Target BuildIshtarNative => _ => _
        .DependsOn(Restore)
        .Executes(() => {
            var runtimes = new[] { "win-x64", "osx-arm64" };
            runtimes.ForEach(runtime => {
                if (!RuntimeInformation.RuntimeIdentifier.Equals(runtime))
                {
                    Log.Warning($"'{runtime} != {RuntimeInformation.RuntimeIdentifier}' is not supported");
                    return;
                }

                var outputDir = OutputDirectory / $"ishtar" / runtime / "native";
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(Ishtar)
                    .SetConfiguration(Configuration.Release)
                    .SetRuntime(runtime)
                    .SetPublishTrimmed(true)
                    .EnableNoRestore());

                // fucking nuke cannot work with native aot
                var targetDir =
                    Ishtar.Directory / "bin" / Configuration.Release /
                                Ishtar.GetProperty("TargetFramework") / runtime / "native";
                CopyDirectoryRecursively(targetDir, outputDir, DirectoryExistsPolicy.Merge);
            });
        });


    Target PackIshtar => _ => _
        .DependsOn(Restore, BuildIshtarDebug)
        .Executes(() => {

            var runtimes = new[] {
                "win-x64",
                "linux-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime =>
            {
                var outputDir = OutputDirectory / $"ishtar" / runtime;
                Compress(outputDir, OutputDirectory / $"vein.runtime.{runtime}.zip");
            });
        });

    #endregion
    bool IsReleaseCommit()
    {
        try
        {
            var b = GitTasks.Git($"tag --contains {Repository.Commit}")
                .FirstOrDefault();
            var tag = b.Text?.Trim();

            return !string.IsNullOrEmpty(tag);
        }
        catch
        {
            return false;
        }
    }

    Target PublishRelease => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(IsReleaseCommit)
        .Executes(async () => {
            var client = new GitHubClient(new ProductHeaderValue("NukeBuild"));
            var tokenAuth = new Credentials(GithubToken);
            client.Credentials = tokenAuth;

            var owner = "vein-lang";
            var repoName = "vein";
            var tagName = GitTasks.Git($"tag --contains {Repository.Commit}").First().Text.Trim();
            Log.Information($"tagName: {tagName}");
            var releaseName = $"Release {tagName}";
            var releaseBody = "";

            var newRelease = new NewRelease(tagName)
            {
                Name = releaseName,
                Body = releaseBody,
                Draft = true,
                Prerelease = true
            };

            var release = await client.Repository.Release.Create(owner, repoName, newRelease);

            var assets = Directory.GetFiles(OutputDirectory, "*.zip");
            foreach (var asset in assets)
            {
                var assetUpload = new ReleaseAssetUpload
                {
                    FileName = Path.GetFileName(asset),
                    ContentType = "application/zip",
                    RawData = File.OpenRead(asset)
                };

                await client.Repository.Release.UploadAsset(release, assetUpload);
            }
        });

    Target PublishWorkloads => _ => _
        .DependsOn(Clean, PackIshtar, BuildVeinc)
        .OnlyWhenDynamic(() => HasPublishWorkloads)
        .Executes(() => {
            Log.Information($"GOING EXECUTE WORKLOADS");
        });

    Target Pack => _ => _
        .DependsOn(PackIshtar, BuildVeinc, BuildRune, PublishWorkloads)
        .Executes();

    Target Compile => _ => _
        .DependsOn(Clean, PublishRelease)
        .Produces(OutputDirectory / "*.zip")
        .Executes(() => {
            Log.Information($"Success building");
        });

    void Compress(AbsolutePath sourceDir, AbsolutePath zipFile, Func<AbsolutePath, bool> filter = null)
    {
        if (zipFile.Exists())
            zipFile.DeleteFile();
        sourceDir.ZipTo(
            zipFile,
            filter: filter,
            compressionLevel: CompressionLevel.SmallestSize);
        Log.Information($"{zipFile}");
    }
}
