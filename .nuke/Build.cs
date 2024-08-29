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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using GlobExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NuGet.Versioning;
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
using vein.cmd;
using vein.compiler.shared;
using vein.json;
using vein.project;
using Nuke.Common.Tools.DotCover;
using static Nuke.Common.Tools.DotCover.DotCoverTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotCover.DotCoverTasks;
using Sentry.Protocol;

[GitHubActions("build_nuke", GitHubActionsImage.UbuntuLatest, AutoGenerate = false,
    On = [GitHubActionsTrigger.Push],
    ImportSecrets = ["VEIN_API_KEY", "CODE_MAID_PAT"],
    EnableGitHubToken = true)]
class Build : NukeBuild
{
    public static int Main ()
    {
        Environment.SetEnvironmentVariable("VEINC_NOVID", "1", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("NO_COLOR", "1", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("NO_CONSOLE", "1", EnvironmentVariableTarget.Process);
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Culture = CultureInfo.InvariantCulture,
            Converters = new List<JsonConverter>()
            {
                new FileInfoSerializer(),
                new StringEnumConverter(),
                new WorkloadKeyContactConverter(),
                new PackageKeyContactConverter(),
                new WorkloadPackageBaseConverter(),
                new PlatformKeyContactConverter(),
                new PackageKindKeyContactConverter(),
                new WorkloadConverter(),
                new DictionaryAliasesConverter()
            }
        };

        return Execute<Build>(x => x.Compile);
    }

    [GitRepository] readonly GitRepository Repository;

    GitHubActions GitHubActions => GitHubActions.Instance;

    [Solution] readonly Solution Solution;

    [GitVersion] readonly GitVersion GitVersion;
    [Description("Mark build as publish workloads")]
    [Parameter] readonly bool HasPublishWorkloads;

    AbsolutePath OutputDirectory => RootDirectory / "output";


    SolutionFolder Tools => Solution.GetSolutionFolder("tools");
    SolutionFolder Libs => Solution.GetSolutionFolder("libs");
    SolutionFolder Backends => Solution.GetSolutionFolder("backends");

    Project Test_IshtarCollections => Solution.GetProject("ishtar_collections_test");
    Project Test_Veinc => Solution.GetProject("veinc_test");


    Project Veinc => Tools.GetProject("veinc");
    Project RuneCLI => Tools.GetProject("rune-cli");
    Project Ishtar => Backends.GetProject("ishtar.vm");
    Project VeinStd => Libs.GetProject("vein.std");


    [Parameter] [Secret] readonly string CodeMaidPat;
    [Parameter] [Secret] readonly string VeinApiKey;

    AbsolutePath WorkloadRuntime => RootDirectory / "workloads" / "runtime";
    AbsolutePath WorkloadCompiler => RootDirectory / "workloads" / "compiler";



    const string TestResultsXmlSuffix = "_TestResults.xml";

    IEnumerable<string> TestAssemblies => [OutputDirectory / "test" / "ishtar_collections_test.dll"];


    Target TestWithCoverage => _ => _
        .DependsOn(BuildTest)
        .Executes(() => {
            
            DotCoverCover(GetDotCoverSettings, Environment.ProcessorCount);
        });
    Target BuildTest => _ => _
        .DependsOn(Restore)
    .Executes(() => {
            var outputDir = OutputDirectory / $"test";
            outputDir.CreateOrCleanDirectory();
            DotNetBuild(c => c
                .SetProjectFile(Test_IshtarCollections)
                .SetConfiguration(Configuration.Debug)
                .SetOutputDirectory(outputDir)
                .EnableNoRestore());
            DotNetBuild(c => c
                .SetProjectFile(Test_Veinc)
                .SetConfiguration(Configuration.Debug)
                .SetOutputDirectory(outputDir)
                .EnableNoRestore());
    });

    IEnumerable<DotCoverCoverSettings> GetDotCoverSettings(DotCoverCoverSettings settings) =>
        TestAssemblies.Select(testAssembly => new DotCoverCoverSettings()
            .SetTargetExecutable(ToolPathResolver.GetPathExecutable("dotnet"))
            .SetTargetWorkingDirectory(OutputDirectory)
            .SetTargetArguments($"test --test-adapter-path:. {testAssembly}  --logger trx;LogFileName={testAssembly}{TestResultsXmlSuffix}")
            .SetFilters(
                "+:MyProject")
            .SetAttributeFilters(
                "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
                "System.CodeDom.Compiler.GeneratedCodeAttribute")
            .SetOutputFile(GetDotCoverOutputFile(testAssembly)));


    AbsolutePath GetDotCoverOutputFile(string testAssembly)
        => OutputDirectory / $"dotCover_{Path.GetFileName(testAssembly)}.dcvr";



    //Target Clean => _ => _
    //    .Before(Restore)
    //    .Executes(() =>
    //    {
    //        OutputDirectory.CreateOrCleanDirectory();
    //        DotNetClean(c => c
    //            .SetProject(Solution));
    //    });

    Target Restore => _ => _
        .Executes(() => {
            DotNetRestore(c => c
                .SetProjectFile(Solution));
        });


    Target BuildVeinStd => _ => _
        .DependsOn(BuildVeinc)
        .Executes(async () =>
        {
            var file = new FileInfo(VeinStd.Directory / "std" / "std.vproj");
            var project = VeinProject.LoadFrom(file);

            Log.Information($"Project '{project.Name}' with version '{project.Version}'");
            Log.Information($"Override version '{project.Version}' to '{GitVersion.AssemblySemVer}'");


            var version = IsReleaseCommit() ?
                $"{GitVersion.LegacySemVer}" :
                $"{GitVersion.LegacySemVer}-preview.{GitVersion.BuildMetaData}";


            try
            {
                var result = await new CompileCommand().ExecuteAsync(null, new CompileSettings()
                {
                    GeneratePackageOutput = true,
                    IgnoreCache = true,
                    DisableOptimization = true,
                    OverrideVersion = version,
                    Project = file.FullName
                }, project);

                if (result is not 0)
                {
                    throw new Exception("Failed publish package");
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        });

    Target PublishVeinStd => _ => _
        .Requires(() => VeinApiKey)
        .DependsOn(BuildVeinStd)
        .Executes(async () =>
        {
            var file = new FileInfo(VeinStd.Directory / "std" / "std.vproj");
            var project = VeinProject.LoadFrom(file);

            Log.Information($"Project '{project.Name}' with version '{project.Version}'");
            Log.Information($"Override version '{project.Version}' to '{GitVersion.AssemblySemVer}'");

            var version = IsReleaseCommit() ?
                $"{GitVersion.LegacySemVer}" :
                $"{GitVersion.LegacySemVer}-preview.{GitVersion.BuildMetaData}";

            var result = await new PublishCommand().ExecuteAsync(null, new PublishCommandSettings()
            {
                Project = VeinStd.Directory / "std" / "std.vproj",
                ApiKey = VeinApiKey,
                OverrideVersion = version
            },project);

            if (result is not 0)
            {
                throw new Exception("Failed publish package");
            }
        });

    #region Rune

    Target BuildRune => _ => _
        .DependsOn(Restore)
        .Produces(OutputDirectory / "*.zip")
        .Executes(() => {
            var runtimes = new[]
            {
                "win-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime => {
                var outputDir =  OutputDirectory / $"rune" / runtime;
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(RuneCLI)
                    .SetConfiguration(Configuration.Release)
                    .EnablePublishSingleFile()
                    .SetPublishTrimmed(true)
                    .SetFramework("net8.0")
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
                    .SetPublishTrimmed(true)
                    .SetFramework("net8.0")
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
                "osx-arm64"
            };
            runtimes.ForEach(runtime => {
                var outputDir =  OutputDirectory / $"ishtar" / runtime;
                outputDir.CreateOrCleanDirectory();
                DotNetPublish(c => c
                    .SetProject(Ishtar)
                    .SetConfiguration(Configuration.Debug)
                    .EnablePublishSingleFile()
                    .EnableSelfContained()
                    .SetFramework("net8.0")
                    .SetPublishTrimmed(true)
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
                    .SetFramework("net8.0")
                    .SetPublishReadyToRun(true)
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
        .Executes(() =>
        {
            var runtimes = new[] {
                "win-x64",
                "osx-arm64"
            };
            runtimes.ForEach(runtime =>
            {
                var outputDir = OutputDirectory / $"ishtar" / runtime;
                Compress(outputDir, OutputDirectory / $"vein.runtime.{runtime}.zip");
            });
        });

    #endregion

    bool? cachedValue;
    bool IsReleaseCommit()
    {
        if (cachedValue is not null)
            return cachedValue.Value;

        try
        {
            var b = GitTasks.Git($"tag --contains {Repository.Commit}")
                .FirstOrDefault();
            var tag = b.Text?.Trim();

            var result = !string.IsNullOrEmpty(tag);
            cachedValue = result;
            return result;
        }
        catch
        {
            cachedValue = false;
            return false;
        }
    }

    Target PublishRelease => _ => _
        .DependsOn(Pack, BuildVeinStd)
        .OnlyWhenDynamic(IsReleaseCommit)
        .Requires(() => CodeMaidPat)
        .Executes(async () => {
            var client = new GitHubClient(new ProductHeaderValue("NukeBuild"));
            var tokenAuth = new Credentials(CodeMaidPat);
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

            var stdAsset = Directory.GetFiles(VeinStd.Directory / "std" / "bin", "*.shard");

            var assets = Directory.GetFiles(OutputDirectory, "*.zip");
            foreach (var asset in assets.Concat(stdAsset))
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
        .DependsOn(PackIshtar, BuildVeinc)
        .OnlyWhenDynamic(() => HasPublishWorkloads)
        .Executes(() => {
            Log.Information($"GOING EXECUTE WORKLOADS");
        });

    Target Pack => _ => _
        .DependsOn(PackIshtar, BuildVeinc, BuildRune, PublishWorkloads)
        .Executes();

    Target Compile => _ => _
        .DependsOn(PublishRelease, BuildVeinStd, PublishVeinStd)
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
