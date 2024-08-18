namespace vein.cmd;

using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Expressive;
using Flurl.Http;
using NuGet.Versioning;
using Org.BouncyCastle.Bcpg;
using project;
using project.shards;
using Spectre.Console;
using Spectre.Console.Cli;
using styles;
using vein;

[ExcludeFromCodeCoverage]
public class InstallWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required RunePackageKey PackageName { get; init; }
    [Description("Package version")]
    [CommandOption("--manifest", IsHidden = true)]
    public string? ManifestFile { get; init; }
}

public record WorkloadInstallingContext(
    ShardRegistryQuery query,
    ShardStorage storage,
    WorkloadDb workloadDb,
    PackageKey PackageName,
    string? PackageVersion,
    string? manifestFile);

public class InstallWorkloadCommand(ShardRegistryQuery query, ShardStorage storage, WorkloadDb workloadDb) : AsyncCommand<InstallWorkloadCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, InstallWorkloadCommandSettings settings) =>
        await WorkloadRegistryLoader.InstallWorkloadAsync(new(query, storage, workloadDb,
            new PackageKey(settings.PackageName.Name),
            settings.PackageName.Version,
            settings.ManifestFile));
}


public class WorkloadRegistryLoader(ShardRegistryQuery query, ShardStorage storage, WorkloadManifest manifest, DirectoryInfo directory, WorkloadDb workloadDb)
{
    private readonly SymlinkCollector Symlink = new(SecurityStorage.RootFolder);
    private static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");

    public static async Task<int> InstallWorkloadAsync(WorkloadInstallingContext context)
    {
        var (query, storage, workloadDb, PackageName, PackageVersion, manifestFile) = context;
        var name = PackageName.key;
        var version = PackageVersion ?? "latest";
        var manifest = default(WorkloadManifest);
        using var tag = ScopeMetric.Begin("install.workload")
            .WithWorkload(name, version);


        if (string.IsNullOrEmpty(manifestFile))
        {
            var result = await query.FindByName(name, version);

            if (result is null)
            {
                Log.Error($"Workload package [orange3]'{name}@{version}'[/] not found in vein gallery.");
                return -1;
            }

            result = await ProgressWithTask.Progress(
                (x) => query.DownloadShardAsync(result, CancellationToken.None, x),
                $"download [orange3]'{name}@{version}'[/] workload... {{%bytes}}");

            if (result is null)
            {
                Log.Error($"Workload package [orange3]'{name}@{version}'[/] not found in vein gallery.");
                return -1;
            }
            manifest = await storage.GetWorkloadManifestAsync(result);

            if (manifest is null)
            {
                Log.Error($"Workload package [orange3]'{name}@{version}'[/] is corrupted.");
                return -1;
            }

            version = manifest.Version.ToNormalizedString();
        }
        else
        {
            manifest = await WorkloadManifest.OpenAsync(manifestFile.ToFile());
            version = manifest.Version.ToNormalizedString();
        }


        var tagFolder = WorkloadDirectory
                    .SubDirectory(name)
                    .SubDirectory(version);
        static void RecursiveDelete(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
                return;
            foreach (var dir in baseDir.EnumerateDirectories()) RecursiveDelete(dir);
            baseDir.Delete(true);
        }
        if (tagFolder.Exists)
        {
            RecursiveDelete(tagFolder);
            tagFolder.Ensure();
        }

        var loader = new WorkloadRegistryLoader(query, storage, manifest, tagFolder, workloadDb);

        if (await loader.InstallManifestForCurrentOS())
        {
            // save latest version of installed
            await WorkloadDirectory
                .SubDirectory(name).File("latest.version").WriteAllTextAsync(version);
            Log.Info($"[green]Success[/] install [orange3]'{name}@{version}'[/] workload into [orange3]'global'[/].");
            return 0;
        }
        Log.Error($"[red]Failed[/] install [orange3]'{name}@{version}'[/] workload.");
        tagFolder.Delete(true);
        return -1;
    }


    public async Task<bool> InstallManifestForCurrentOS()
    {
        await directory.Ensure().File("workload.manifest.json").WriteAllTextAsync(manifest.SaveAsString());

        await Task.Delay(1000);
        var result = true;

        foreach (var (id, workload) in manifest.Workloads)
        {
            try
            {
                result &= await InstallWorkloadForCurrentOS(workload, directory);
            }
            catch (PlatformNotSupportedException e)
            {
                Log.Error($"Failed install workload '[red]{e.Message.EscapeMarkup()}[/]'");
                return false;
            }
            catch (SkipExecution)
            {
                return false;
            }
            catch (Exception e)
            {
                Log.Error($"bad workload package");
                AnsiConsole.WriteException(e);
                return false;
            }
        }
        return result;
    }


    private async Task<bool> InstallDependencies(WorkloadPackage package)
    {
        foreach (var (key, version) in package.Dependencies)
        {
            var loader = await InstallWorkloadAsync(new WorkloadInstallingContext(query, storage, workloadDb, key, version.ToNormalizedString(), null));

            if (loader != 0) return false;
        }

        return true;
    }

    private async Task<bool> InstallWorkloadForCurrentOS(Workload workload, DirectoryInfo targetFolder)
    {
        var result = true;
        foreach (var x in workload.Packages)
        {
            var package = manifest.Packages[x];

            result &= await InstallDependencies(package);

            if (!result)
                return false;

            result &= package.Kind switch
            {
                { } when package.Kind == PackageKindKey.Sdk
                    => await InstallSdk(package, targetFolder),
                { } when package.Kind == PackageKindKey.Tool
                    => await InstallTool(package, targetFolder),
                { } when package.Kind == PackageKindKey.Template
                    => await InstallTemplate(package, targetFolder),
                { } when package.Kind == PackageKindKey.Frameworks
                    => await InstallFrameworks(package, targetFolder),
                _ => throw new PlatformNotSupportedException("unknown kind of workload")
            };
        }

        return result;
    }

    private async Task<bool> InstallSdk(WorkloadPackage sdk, DirectoryInfo targetFolder)
    {
        var currentOs = PlatformKey.GetCurrentPlatform();

        if (!sdk.Aliases.TryGetValue(currentOs, out var alias))
            throw new PlatformNotSupportedException($"Aliases workload not found for '{currentOs}' platform");

        if (alias.StartsWith("http:"))
            throw new PlatformNotSupportedException($"Workload installer not support http scheme. please use https");
        if (alias.StartsWith("file://"))
        {
#if DEBUG
            // for testing reason allow use file scheme
            await using var zip = new PackageArchive(alias.Replace(@"file://", "").ToFile());

            zip.ExtractTo(targetFolder);
            return true;
#else
            throw new PlatformNotSupportedException($"Workload installer not support file scheme. please use https");
#endif
        }

        if (alias.StartsWith("nuget://"))
        {
            var fullAlias = alias.Replace("nuget://", "");

            var packageName = fullAlias.Split('@').First();
            var packageVersion = fullAlias.Split('@').Last();


            var nugetDirectory = await storage.EnsureNuGetPackage(packageName, NuGetVersion.Parse(packageVersion));

            CopyFilesRecursively(nugetDirectory, targetFolder);


            var pathBuilder = new StringBuilder();

            foreach (var packageSdk in sdk.Definition.OfType<WorkloadPackageSdk>())
            {

                if (packageSdk.Aliases.TryGetValue(currentOs, out alias))
                {
                    await workloadDb.RegistrySdkAsync(packageSdk.SdkTarget, alias.Replace("root://", ""), targetFolder);
                    pathBuilder.AppendLine($"{packageSdk.SdkTarget},{alias.Replace("root://", "")};");
                }
            }


            await targetFolder.File("sdk.target").WriteAllTextAsync(pathBuilder.ToString());


            return true;
        }

        return false;
    }

    private static void CopyFilesRecursively(DirectoryInfo sourcePath, DirectoryInfo targetPath)
    {
        foreach (string dirPath in Directory.GetDirectories(sourcePath.FullName, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath.FullName, targetPath.FullName));
        foreach (string newPath in Directory.GetFiles(sourcePath.FullName, "*.*", SearchOption.AllDirectories)) File.Copy(newPath, newPath.Replace(sourcePath.FullName, targetPath.FullName), true);
    }

    private async Task<bool> InstallTool(WorkloadPackage pkg, DirectoryInfo targetFolder) =>
        await ProgressWithTask.Progress(
            async (x) => {
                var tools = pkg.Definition.OfType<WorkloadPackageTool>().ToList();

                var packageFolder = await DownloadWorkloadAndInstallPackage(pkg, targetFolder, manifest.Version, x);
                foreach (var tool in tools)
                {
                    await workloadDb.RegistryTool(pkg.name, tool, packageFolder);
                    if (!tool.ExportSymlink)
                        continue;
                    var file = new FileInfo(Symlink.ToExec(tool.ExecPath));
                    var toolExec = packageFolder.Combine(file);
                    Symlink.GenerateSymlink(
                        string.IsNullOrEmpty(tool.OverrideName) ?
                            Path.GetFileNameWithoutExtension(file.Name) :
                            tool.OverrideName, toolExec);
                    await Task.Delay(500);
                }
                return true;
            }, $"unpack [orange3]'{pkg.name.key}'[/] tools {{%bytes}}");

    private async Task<bool> InstallTemplate(WorkloadPackage sdk, DirectoryInfo targetFolder) => false;

    private async Task<bool> InstallFrameworks(WorkloadPackage sdk, DirectoryInfo targetFolder) => false;


    private async Task<DirectoryInfo> DownloadWorkloadAndInstallPackage(
        WorkloadPackage package, DirectoryInfo targetFolder, NuGetVersion version, IProgress<(int total, int speed)> progress)
    {
        var currentOs = PlatformKey.GetCurrentPlatform();

        if (!package.Aliases.TryGetValue(currentOs, out var alias))
            throw new PlatformNotSupportedException($"Aliases workload not found for '{currentOs}' platform");

        var packageFolder = targetFolder.SubDirectory($"{package.name.key}").Ensure();

        if (alias.StartsWith("http://"))
            throw new PlatformNotSupportedException($"Workload installer not support http scheme. please use https");
        if (alias.StartsWith("file://"))
        {
#if DEBUG
            // for testing reason allow use file scheme
            await using var zip = new PackageArchive(alias.Replace(@"file://", "").ToFile());

            zip.ExtractTo(packageFolder);
            return packageFolder;
#else
            throw new PlatformNotSupportedException($"Workload installer not support file scheme. please use https");
#endif
        }

        if (alias.StartsWith("https://"))
        {
            var tempFolder = Path
                .GetTempPath()
                .ToDirectory()
                .Combine(Guid.NewGuid().ToString("N").ToDirectory());

            try
            {
                var zipFile = await alias
                    .WithTimeout(60)
                    .DownloadFileAsync(tempFolder.FullName, $"workload.{package.name.key}.zip", progress: progress)
                    .ToFileAsync();

                await using var zip = new PackageArchive(zipFile);

                zip.ExtractTo(packageFolder);
            }
            catch (FlurlHttpException e)
            {
                Log.Error($"Trying download [grey]'{alias.EscapeMarkup()}'[/], but returned '{e.StatusCode}' status code.");
                throw new SkipExecution(e);
            }
            return packageFolder;
        }

        var shardManifest = await query.FindByName(alias, version.ToNormalizedString(), true);

        if (shardManifest is null)
            throw new PackageDefinedInWorkloadNotFoundException();

        await query.DownloadShardAsync(shardManifest, CancellationToken.None, progress);
        var shardFile = query.GetLocalShardFile(shardManifest);

        var shard = await Shard.OpenAsync(shardFile);
        shard.ExtractTo(packageFolder);

        return packageFolder;
    }

}


public class PackageDefinedInWorkloadNotFoundException : Exception;
public class SkipExecution(Exception e) : Exception(null, e);
