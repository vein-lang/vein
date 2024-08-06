namespace vein;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreLinq;
using Newtonsoft.Json;
using NuGet.Versioning;
using project;

public class ShardStorage : IShardStorage
{
    public static readonly DirectoryInfo RootFolder =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein",
            "shards"));

    public static readonly DirectoryInfo RootFolderWorkloads =
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vein", "workloads"));

    public void EnsureDefaultDirectory()
    {
        if (!RootFolder.Exists) RootFolder.Create();
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

    public DirectoryInfo GetPackageSpace(string name, NuGetVersion version)
        => RootFolder
            .SubDirectory(name)
            .SubDirectory(version.ToNormalizedString());

    public void Prune() =>
        RootFolder.EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Pipe(x => x.Delete());

    public List<NuGetVersion> GetAvailableVersions(string name) =>
        RootFolder
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

    public RegistryPackage GetManifest(string name, NuGetVersion version)
    {
        if (!IsAvailable(name, version))
            return null;

        var json = GetPackageSpace(name, version).File("manifest.json");
        return JsonConvert.DeserializeObject<RegistryPackage>(json.ReadToEnd());
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

    RegistryPackage GetManifest(string name, NuGetVersion version);
}
