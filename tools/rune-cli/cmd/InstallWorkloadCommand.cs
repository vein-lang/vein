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

[ExcludeFromCodeCoverage]
public class InstallWorkloadCommandSettings : CommandSettings
{
    [Description("A package name.")]
    [CommandArgument(0, "[PACKAGE]")]
    public required string PackageName { get; init; }

    [Description("Package version")]
    [CommandOption("--version")]
    public string? PackageVersion { get; init; }

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
    string? manifestFile,
    ProgressContext ctx);

public class InstallWorkloadCommand(ShardRegistryQuery query, ShardStorage storage, WorkloadDb workloadDb) : AsyncCommandWithProgress<InstallWorkloadCommandSettings>
{
    public override async Task<int> ExecuteAsync(ProgressContext context, InstallWorkloadCommandSettings settings)
        => await WorkloadRegistryLoader.InstallWorkloadAsync(new (query, storage, workloadDb,
            new PackageKey(settings.PackageName),
            settings.PackageVersion,
            settings.ManifestFile,
            context));
}


public class WorkloadRegistryLoader(ShardRegistryQuery query, ShardStorage storage, WorkloadManifest manifest, DirectoryInfo directory, WorkloadDb workloadDb, ProgressContext ctx)
{
    private readonly SymlinkCollector Symlink = new(SecurityStorage.RootFolder);
    private static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");

    public static async Task<int> InstallWorkloadAsync(WorkloadInstallingContext context)
    {
        var (query, storage, workloadDb, PackageName, PackageVersion, manifestFile, ctx) = context;
        using var task = ctx.AddTask($"fetch [orange3]'{PackageName.key}'[/] workload...")
            .IsIndeterminate();

        var name = PackageName.key;
        var version = PackageVersion ?? "latest";
        var manifest = default(WorkloadManifest);

        if (string.IsNullOrEmpty(manifestFile))
        {
            var result = await query.FindByName(name, version);

            if (result is null)
            {
                task.FailTask();
                Log.Error($"Workload package [orange3]'{name}@{version}'[/] not found in vein gallery.");
                return -1;
            }

            task.Description($"download [orange3]'{PackageName}'[/] workload...");

            await query.DownloadShardAsync(result);

            manifest = await storage.GetWorkloadManifestAsync(result);

            if (manifest is null)
            {
                task.FailTask();
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

        if (tagFolder.Exists)
        {
            task.FailTask();
            Log.Error($"Workload package [orange3]'{name}@{version}'[/] already installed, maybe u need [gray]'workload uninstall'[/]?");
            return -1;
        }



        var loader = new WorkloadRegistryLoader(query, storage, manifest, tagFolder, workloadDb, ctx);

        if (await loader.InstallManifestForCurrentOS())
        {
            // save latest version of installed
            WorkloadDirectory
                .SubDirectory(name).File("latest.version").WriteAllText(version);
            Log.Info($"[green]Success[/] install [orange3]'{name}@{version}'[/] workload into [orange3]'global'[/].");
            return 0;
        }
        Log.Error($"[red]Failed[/] install [orange3]'{name}@{version}'[/] workload.");
        tagFolder.Delete(true);
        task.FailTask();

        return -1;
    }





    public async Task<bool> InstallManifestForCurrentOS()
    {
        directory.Ensure().File("workload.manifest.json").WriteAllText(manifest.SaveAsString());

        var enums = manifest.Workloads.Select(x => (x, ctx.AddTask($"Downloading workload '{x.Value.name.key}'"))).ToList();
        await Task.Delay(1000);
        var result = true;

        foreach (var ((id, workload), task) in enums)
        {
            using var _ = task.IsIndeterminate();
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
                return false;
            }
        }

        enums.ForEach(x => x.Item2.StopTask());

        return result;
    }


    private async Task<bool> InstallDependencies(WorkloadPackage package)
    {
        foreach (var (key, version) in package.Dependencies)
        {
            var loader = await InstallWorkloadAsync(new WorkloadInstallingContext(query, storage, workloadDb, key, version.ToNormalizedString(), null, ctx));

            if (loader != 0) return false;
        }

        return true;
    }

    private async Task<bool> InstallWorkloadForCurrentOS(Workload workload, DirectoryInfo targetFolder)
    {
        var enums = workload.Packages.Select(x => (x, ctx.AddTask($"Processing '{x.key}' package..."))).ToList();
        var result = true;
        foreach (var (x, task) in enums)
        {
            using var _ = task.IsIndeterminate();

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

        enums.ForEach(x => x.Item2.StopTask());

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

    private async Task<bool> InstallTool(WorkloadPackage pkg, DirectoryInfo targetFolder)
    {
        var tools = pkg.Definition.OfType<WorkloadPackageTool>().ToList();

        using var task = ctx.AddTask($"Downloading '{pkg.name.key}@{manifest.Version.ToNormalizedString()}'");
        
        var packageFolder = await DownloadWorkloadAndInstallPackage(pkg, targetFolder, manifest.Version);
        task.Description("linking tools...");
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
    }

    private async Task<bool> InstallTemplate(WorkloadPackage sdk, DirectoryInfo targetFolder) => false;

    private async Task<bool> InstallFrameworks(WorkloadPackage sdk, DirectoryInfo targetFolder) => false;


    private async Task<DirectoryInfo> DownloadWorkloadAndInstallPackage(
        WorkloadPackage package, DirectoryInfo targetFolder, NuGetVersion version)
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
                    .DownloadFileAsync(tempFolder.FullName, $"workload.{package.name.key}.zip")
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

        await query.DownloadShardAsync(shardManifest);
        var shardFile = query.GetLocalShardFile(shardManifest);

        var shard = await Shard.OpenAsync(shardFile);
        shard.ExtractTo(packageFolder);

        return packageFolder;
    }

}


public class PackageDefinedInWorkloadNotFoundException : Exception;
public class SkipExecution(Exception e) : Exception(null, e);
