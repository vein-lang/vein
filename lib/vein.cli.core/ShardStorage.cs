namespace vein;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using MoreLinq;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using project;

public class ShardStorage : IShardStorage
{
    public static readonly DirectoryInfo VeinRootFolder =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein"));

    public static readonly DirectoryInfo ShardRootFolder =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein",
            "shards"));

    public static readonly DirectoryInfo RootFolderWorkloads =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein", "workloads"));

    public static readonly DirectoryInfo RootFolderNugets =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein", "nugets"));

    private void EnsureDefaultDirectory()
    {
        if (!ShardRootFolder.Exists) ShardRootFolder.Create();
        if (!RootFolderWorkloads.Exists) RootFolderWorkloads.Create();
        if (!RootFolderNugets.Exists) RootFolderNugets.Create();
    }


    public DirectoryInfo EnsureSpace(NuspecReader package)
    {
        EnsureDefaultDirectory();

        var space = GetPackageSpace(package);

        space.Create();

        return space;
    }

    public DirectoryInfo EnsureSpace(RegistryPackage package)
    {
        EnsureDefaultDirectory();

        var space = GetPackageSpace(package);

        space.Create();

        return space;
    }

    public bool IsAvailable(RegistryPackage package)
        => IsAvailable(package.Name, package.Version);

    public bool IsAvailable(string name, NuGetVersion version)
        => GetShardByName(name, version).Exists;

    public FileInfo GetShardByName(string name, NuGetVersion version)
        => GetPackageSpace(name, version).File(TemplateName(name, version));

    public FileInfo GetShardByName(RegistryPackage package)
        => GetShardByName(package.Name, package.Version);

    public DirectoryInfo GetPackageSpace(RegistryPackage package)
        => GetPackageSpace(package.Name, package.Version);

    public DirectoryInfo GetPackageSpace(NuspecReader package)
        => ToNugetFolder(package.GetId(), package.GetVersion());

    public DirectoryInfo GetPackageSpace(string name, NuGetVersion version)
        => ShardRootFolder
            .SubDirectory(name)
            .SubDirectory(version.ToNormalizedString());

    public void Prune() =>
        ShardRootFolder.EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Pipe(x => x.Delete());

    public List<NuGetVersion> GetAvailableVersions(string name) =>
        ShardRootFolder
            .SubDirectory(name)
            .EnumerateDirectories()
            .Where(x => NuGetVersion.TryParse(x.Name, out _))
            .Select(x => NuGetVersion.Parse(x.Name))
            .ToList();

    public List<FileInfo> GetBinaries(string name, NuGetVersion version)
    {
        var bin = GetPackageSpace(name, version).SubDirectory("lib");
        if (bin.Exists) return bin.EnumerateFiles("*.wll").ToList();
        return new();
    }


    public async Task<List<WorkloadManifest>> GetInstalledWorkloads()
    {
        var workloads = RootFolderWorkloads;

        if (!workloads.Exists)
            return new ();

        // maybe poor perf
        //var loader = workloads.GetDirectories()
        //    .Where(x => x.File("latest.version").Exists)
        //    .Select(x => (x, x.SubDirectory(x.File("latest.version").ReadToEnd())))
        //    .Select(x => (x.x.Name, x.Item2.File("workload.manifest.json")))
        //    .Select(x => new { name = x.Name, manifest = x.Item2 })
        //    .Select(x => new { task = WorkloadManifest.OpenAsync(x.manifest), x.name });

        var loader = workloads.EnumerateFiles("workload.manifest.json", SearchOption.AllDirectories)
            .Select(WorkloadManifest.OpenAsync);
        
        var manifests = await Task.WhenAll(loader);


        return manifests.ToList();
    }


    public Task<WorkloadManifest?> GetWorkloadManifestAsync(RegistryPackage package)
        => GetWorkloadManifestAsync(package.Name, package.Version);

    public async Task<WorkloadManifest?> GetWorkloadManifestAsync(string name, NuGetVersion version)
    {
        var bin = GetPackageSpace(name, version).File("workload.manifest.json");
        if (bin.Exists) return await WorkloadManifest.OpenAsync(bin);
        return null;
    }

    public List<FileInfo> GetBinaries(RegistryPackage package)
        => GetBinaries(package.Name, package.Version);

    public string TemplateName(string name, NuGetVersion version) =>
        $"{name}-{version}.shard";
    public string TemplateName(RegistryPackage package) =>
        TemplateName(package.Name, package.Version);

    public RegistryPackage? GetManifest(string name, NuGetVersion version)
    {
        if (!IsAvailable(name, version))
            return null;

        var json = GetPackageSpace(name, version).File("manifest.json");
        return JsonConvert.DeserializeObject<RegistryPackage>(json.ReadToEnd())!;
    }


    public DirectoryInfo ToNugetFolder(string packageId, NuGetVersion version) =>
        RootFolderNugets
            .SubDirectory(packageId)
            .SubDirectory(version.ToNormalizedString());

    public bool NuGetPackageHasInstalled(string packageId, NuGetVersion version) =>
        ToNugetFolder(packageId, version)
            .File("package.nupkg").Exists;

    public async Task<DirectoryInfo> EnsureNuGetPackage(string packageId, NuGetVersion version)
    {
        var targetDirectory = ToNugetFolder(packageId, version);
        var selfFolder = targetDirectory.SubDirectory(".self");
        if (NuGetPackageHasInstalled(packageId, version))
            return selfFolder;

        CancellationToken cancellationToken = CancellationToken.None;
        var logger = NuGet.Common.NullLogger.Instance;
        SourceCacheContext cache = new SourceCacheContext();
        SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        using MemoryStream packageStream = new MemoryStream();

        await resource.CopyNupkgToStreamAsync(
            packageId,
            version,
            packageStream,
            cache,
            logger,
            cancellationToken);

        using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
        NuspecReader nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);

        
        EnsureSpace(nuspecReader);

        var nuspecStream = targetDirectory.File("nuspec.config").OpenWrite();

        await nuspecReader.Xml.SaveAsync(nuspecStream, SaveOptions.None, cancellationToken);

        nuspecStream.Close();
        await nuspecStream.DisposeAsync();

        var NuPkgStream = targetDirectory.File("package.nupkg").OpenWrite();

        await packageStream.CopyToAsync(NuPkgStream, cancellationToken);

        NuPkgStream.Close();

        await NuPkgStream.DisposeAsync();

        

        var files = packageReader.GetFiles().ToList();
        packageReader.CopyFiles(selfFolder.Ensure().FullName, files, (file, path, stream) =>
        {
            new FileInfo(path)!.Directory!.Ensure();
            using var fi = File.OpenWrite(path);
            stream.CopyTo(fi);
            return path;
        }, logger, cancellationToken);


        return selfFolder;
    }
}


public interface IShardStorage
{
    bool IsAvailable(RegistryPackage package);
    bool IsAvailable(string name, NuGetVersion version);
    DirectoryInfo EnsureSpace(RegistryPackage package);
    FileInfo GetShardByName(string name, NuGetVersion version);
    FileInfo GetShardByName(RegistryPackage package);
    DirectoryInfo GetPackageSpace(RegistryPackage package);
    DirectoryInfo GetPackageSpace(string name, NuGetVersion version);
    void Prune();

    List<NuGetVersion> GetAvailableVersions(string name);

    List<FileInfo> GetBinaries(string name, NuGetVersion version);
    List<FileInfo> GetBinaries(RegistryPackage package);

    string TemplateName(string name, NuGetVersion version);

    string TemplateName(RegistryPackage package);

    RegistryPackage? GetManifest(string name, NuGetVersion version);
}
