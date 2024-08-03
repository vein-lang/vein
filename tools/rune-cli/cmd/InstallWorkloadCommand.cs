namespace vein.cmd;

using System.ComponentModel;
using System.IO.Compression;
using Flurl.Http;
using NuGet.Versioning;
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
    public required string PackageName { get; set; }

    [Description("Package version")]
    [CommandOption("--version")]
    public string? PackageVersion { get; set; }

    [Description("Package version")]
    [CommandOption("--manifest", IsHidden = true)]
    public string? ManifestFile { get; set; }
}

public class InstallWorkloadCommand : AsyncCommandWithProgress<InstallWorkloadCommandSettings>
{
    public static readonly DirectoryInfo WorkloadDirectory = SecurityStorage.RootFolder.SubDirectory("workloads");
    public static readonly Uri VEIN_GALLERY = new("https://api.vein-lang.org/");
    public static readonly ShardStorage Storage = new();

    public override async Task<int> ExecuteAsync(ProgressContext context, InstallWorkloadCommandSettings settings)
    {
        using var task = context.AddTask($"fetch [orange3]'{settings.PackageName}'[/] workload...")
            .IsIndeterminate();


        await Task.Delay(1000);

        var name = settings.PackageName;
        var version = settings.PackageVersion ?? "latest";
        var manifest = default(WorkloadManifest);

        if (string.IsNullOrEmpty(settings.ManifestFile))
        {
            var query = new ShardRegistryQuery(VEIN_GALLERY).WithStorage(Storage);

            var result = await query.FindByName(name, version);

            if (result is null)
            {
                task.FailTask();
                Log.Error($"Workload package [orange3]'{name}@{version}'[/] not found in vein gallery.");
                return -1;
            }

            task.Description($"download [orange3]'{settings.PackageName}'[/] workload...");

            await query.DownloadShardAsync(result);

            manifest = await Storage.GetWorkloadManifestAsync(result);
        }
        else
        {
            manifest = await WorkloadManifest.OpenAsync(settings.ManifestFile.ToFile());
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

        if (manifest is null)
        {
            task.FailTask();
            Log.Error($"Workload package [orange3]'{name}@{version}'[/] is corrupted.");
            return -1;
        }

        var loader = new WorkloadRegistryLoader(manifest, tagFolder, context);

        if (await loader.InstallManifestForCurrentOS())
        {
            // save latest version of installed
            WorkloadDirectory
                .SubDirectory(name).File("latest").WriteAllText(version);
            Log.Info($"[green]Success[/] install [orange3]'{name}@{version}'[/] workload into [orange3]'global'[/].");
            return 0;
        }
        Log.Error($"[red]Failed[/] install [orange3]'{name}@{version}'[/] workload.");
        tagFolder.Delete(true);
        task.FailTask();
        return -1;
    }
}


public class WorkloadRegistryLoader(WorkloadManifest manifest, DirectoryInfo directory, ProgressContext ctx)
{
    public SymlinkCollector Symlink = new(SecurityStorage.RootFolder);
    public async Task<bool> InstallManifestForCurrentOS()
    {
        directory.Ensure().File("manifest.json").WriteAllText(manifest.SaveAsString());

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
                Log.Error($"");
            }
        }

        enums.ForEach(x => x.Item2.StopTask());

        return result;
    }

    public async Task<bool> InstallWorkloadForCurrentOS(Workload workload, DirectoryInfo targetFolder)
    {
        var enums = workload.Packages.Select(x => (x, ctx.AddTask($"Processing '{x.key}' package..."))).ToList();
        var result = true;
        foreach (var (x, task) in enums)
        {
            using var _ = task.IsIndeterminate();

            var package = manifest.Packages[x];

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

    public async Task<bool> InstallSdk(WorkloadPackage sdk, DirectoryInfo targetFolder)
    {
        return false;
    }
    public async Task<bool> InstallTool(WorkloadPackage pkg, DirectoryInfo targetFolder)
    {
        var tools = pkg.Definition.OfType<WorkloadPackageTool>().ToList();

        using var task = ctx.AddTask($"Downloading '{pkg.name.key}@{manifest.Version.ToNormalizedString()}'");
        
        var packageFolder = await DownloadWorkloadAndInstallPackage(pkg, targetFolder, manifest.Version);
        task.Description("linking tools...");
        foreach (var tool in tools)
        {
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
    public async Task<bool> InstallTemplate(WorkloadPackage sdk, DirectoryInfo targetFolder)
    {
        return false;
    }
    public async Task<bool> InstallFrameworks(WorkloadPackage sdk, DirectoryInfo targetFolder)
    {
        return false;
    }


    public async Task<DirectoryInfo> DownloadWorkloadAndInstallPackage(
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

        var query = new ShardRegistryQuery(InstallWorkloadCommand.VEIN_GALLERY);

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
