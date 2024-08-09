namespace vein.project;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NuGet.Versioning;


public record Workload([field: JsonIgnore] WorkloadKey name)
{
    public required List<PlatformKey> Platforms { get; init; } = new();
    public required List<PackageKey> Packages { get; init; } = new();
    public required string Description { get; init; }
}

public record WorkloadPackage([field: JsonIgnore] PackageKey name)
{
    public required PackageKindKey Kind { get; init; }
    [JsonConverter(typeof(DictionaryAliasesConverter))]
    public required Dictionary<PlatformKey, string> Aliases { get; init; } = new();
    public required List<IWorkloadPackageBase> Definition { get; init; } = new();
    [JsonConverter(typeof(DictionaryDependencyConverter))]
    public required Dictionary<PackageKey, NuGetVersion> Dependencies { get; init; } = new();
}

public interface IWorkloadPackageBase;

public record WorkloadPackageTool : IWorkloadPackageBase
{
    public required string ExecPath { get; init; }
    public string? OverrideName { get; set; }
    public required bool ExportSymlink { get; init; }
}

public record WorkloadPackageTemplate : IWorkloadPackageBase
{
    public required string TemplatePath { get; init; }
    public required string PartOf { get; init; }
    public required string Index { get; init; }
}

public record WorkloadPackageFramework : IWorkloadPackageBase
{
    public required string PackageTarget { get; init; }
    public required List<string> ExportPattern { get; init; }
}

public record WorkloadPackageSdk : IWorkloadPackageBase
{
    public required string SdkTarget { get; init; }
    [JsonConverter(typeof(WorkloadSdkAliasesPackageConverter))]
    public required Dictionary<PlatformKey, string> Aliases { get; init; } = new();
}

public record WorkloadManifest
{
    [JsonProperty("name")]
    public required string Name { get; set; }
    [JsonConverter(typeof(NuGetVersionConverter))]
    public required NuGetVersion Version { get; init; }
    [JsonConverter(typeof(WorkloadConverter))]
    public required Dictionary<WorkloadKey, Workload> Workloads { get; init; } = new();
    [JsonConverter(typeof(WorkloadPackageConverter))]
    public required Dictionary<PackageKey, WorkloadPackage> Packages { get; init; } = new();


    public static async Task<WorkloadManifest> OpenAsync(FileInfo bin)
        => JsonConvert.DeserializeObject<WorkloadManifest>(await bin.ReadToEndAsync())!;

    public string SaveAsString() =>
        JsonConvert.SerializeObject(this, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
}

[JsonConverter(typeof(PlatformKeyContactConverter))]
public readonly record struct PlatformKey(string key)
{
    public static readonly PlatformKey Any = new("any");
    public static readonly PlatformKey Windows_x64 = new("win-x64");
    public static readonly PlatformKey Windows_arm64 = new("win-arm64");
    public static readonly PlatformKey Linux_ppc64el = new("linux-ppc64el");
    public static readonly PlatformKey Linux_arm64 = new("linux-arm64");
    public static readonly PlatformKey Linux_x64 = new("linux-x64");
    public static readonly PlatformKey Osx_arm64 = new("osx-arm64");
    public static readonly PlatformKey Osx_x64 = new("osx-x64");

    public static readonly IReadOnlyList<PlatformKey> All = new List<PlatformKey>()
    {
        Any,
        Windows_x64,
        Windows_arm64,
        Linux_ppc64el,
        Linux_x64,
        Linux_arm64,
        Osx_arm64,
        Osx_x64
    }.AsReadOnly();

    public static PlatformKey GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
            return Windows_x64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return Windows_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Ppc64le)
            return Linux_ppc64el;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return Linux_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.X64)
            return Linux_x64;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return Osx_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.X64)
            return Osx_x64;

        throw new PlatformNotSupportedException($"Platform is not supported, only supported '{string.Join(',', All.Select(x => x.key))}'");
    }

    public override string ToString() => key;
}

[JsonConverter(typeof(PackageKeyContactConverter))]
public readonly record struct PackageKey(string key)
{
    public override string ToString() => key;
}

[JsonConverter(typeof(WorkloadKeyContactConverter))]
public readonly record struct WorkloadKey(string key)
{
    public override string ToString() => key;
}

[JsonConverter(typeof(PackageKindKeyContactConverter))]
public readonly record struct PackageKindKey(string key)
{
    public static PackageKindKey Sdk = new("sdk");
    public static PackageKindKey Frameworks = new("frameworks");
    public static PackageKindKey Tool = new("tool");
    public static PackageKindKey Template = new("template");

    public static IReadOnlyList<PackageKindKey> All = new List<PackageKindKey>()
    {
        Frameworks,
        Sdk,
        Tool,
        Template
    }.AsReadOnly();
    public override string ToString() => key;
}
