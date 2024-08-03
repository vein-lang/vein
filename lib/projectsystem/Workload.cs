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


public class WorkloadKeyContactConverter : JsonConverter<WorkloadKey>
{
    public override void WriteJson(JsonWriter writer, WorkloadKey value, JsonSerializer serializer)
        => writer.WriteValue(value.key);

    public override WorkloadKey ReadJson(JsonReader reader, Type objectType, WorkloadKey existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (hasExistingValue)
            throw new NotSupportedException();

        return new ((string)reader.Value);
    }
}
public class PackageKeyContactConverter : JsonConverter<PackageKey>
{
    public override void WriteJson(JsonWriter writer, PackageKey value, JsonSerializer serializer)
        => writer.WriteValue(value.key);

    public override PackageKey ReadJson(JsonReader reader, Type objectType, PackageKey existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (hasExistingValue)
            throw new NotSupportedException();
        return new((string)reader.Value);
    }
}
public class PlatformKeyContactConverter : JsonConverter<PlatformKey>
{
    public override void WriteJson(JsonWriter writer, PlatformKey value, JsonSerializer serializer)
        => writer.WriteValue(value.key);

    public override PlatformKey ReadJson(JsonReader reader, Type objectType, PlatformKey existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (hasExistingValue)
            throw new NotSupportedException();
        return new((string)reader.Value);
    }
}
public class PackageKindKeyContactConverter : JsonConverter<PackageKindKey>
{
    public override void WriteJson(JsonWriter writer, PackageKindKey value, JsonSerializer serializer)
        => writer.WriteValue(value.key);

    public override PackageKindKey ReadJson(JsonReader reader, Type objectType, PackageKindKey existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (hasExistingValue)
            throw new NotSupportedException();
        return new((string)reader.Value);
    }
}

public class WorkloadPackageBaseConverter : JsonConverter<List<IWorkloadPackageBase>>
{
    public override void WriteJson(JsonWriter writer, List<IWorkloadPackageBase> value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var @base in value)
        {
            JObject.FromObject(@base).WriteTo(writer);
        }
        writer.WriteEndArray();
    }

    public override List<IWorkloadPackageBase> ReadJson(JsonReader reader, Type objectType, List<IWorkloadPackageBase> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Expected StartObject token");

        existingValue ??= new List<IWorkloadPackageBase>();

        var jsonArray = JArray.Load(reader);

        foreach (var expression in jsonArray)
        {
            if (expression is not JObject jObj)
                continue;
            if (jObj.ContainsKey("execPath"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageTool>());
                continue;
            }

            if (jObj.ContainsKey("templatePath"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageTemplate>());
                continue;
            }
            if (jObj.ContainsKey("packageTarget"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageFramework>());
                continue;
            }
            throw new JsonSerializationException("Unknown IWorkloadPackageBase type");
        }

        return existingValue;
    }
}
public class WorkloadConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => typeof(Dictionary<WorkloadKey, Workload>).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var workloads = new Dictionary<WorkloadKey, Workload>();
        var jsonObject = JObject.Load(reader);

        foreach (var property in jsonObject.Properties())
        {
            var workload = property.Value.ToObject<Workload>(serializer);
            workloads[new WorkloadKey(property.Name)] = workload;
        }

        return workloads;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var workloads = (Dictionary<WorkloadKey, Workload>)value;
        writer.WriteStartObject();

        foreach (var kvp in workloads)
        {
            writer.WritePropertyName(kvp.Key.key);
            serializer.Serialize(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }
}
public class WorkloadPackageConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => typeof(Dictionary<PackageKey, WorkloadPackage>).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var packages = new Dictionary<PackageKey, WorkloadPackage>();
        var jsonObject = JObject.Load(reader);

        foreach (var property in jsonObject.Properties())
        {
            var package = property.Value.ToObject<WorkloadPackage>(serializer);
            packages[new PackageKey(property.Name)] = package;
        }

        return packages;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var packages = (Dictionary<PackageKey, WorkloadPackage>)value;
        writer.WriteStartObject();

        foreach (var kvp in packages)
        {
            writer.WritePropertyName(kvp.Key.key);
            serializer.Serialize(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }
}

public class DictionaryAliasesConverter : JsonConverter<Dictionary<PlatformKey, string>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<PlatformKey, string> value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key.key);
            serializer.Serialize(writer, kvp.Value);
        }
        writer.WriteEndObject();
    }

    public override Dictionary<PlatformKey, string> ReadJson(JsonReader reader, Type objectType,
        Dictionary<PlatformKey, string> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var packages = new Dictionary<PlatformKey, string>();
        var jsonObject = JObject.Load(reader);

        foreach (var property in jsonObject.Properties())
        {
            var package = property.Value.ToObject<string>(serializer);
            packages[new PlatformKey(property.Name)] = package;
        }

        return packages;
    }
}
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
    public required List<PackageKey> Dependencies { get; init; } = new();
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

}

public record WorkloadManifest
{
    [JsonConverter(typeof(NuGetVersionConverter))]
    public required NuGetVersion Version { get; init; }
    [JsonConverter(typeof(WorkloadConverter))]
    public required Dictionary<WorkloadKey, Workload> Workloads { get; init; } = new();
    [JsonConverter(typeof(WorkloadPackageConverter))]
    public required Dictionary<PackageKey, WorkloadPackage> Packages { get; init; } = new();


    public static async Task<WorkloadManifest> OpenAsync(FileInfo bin)
        => JsonConvert.DeserializeObject<WorkloadManifest>(await bin.ReadToEndAsync());

    public string SaveAsString() =>
        JsonConvert.SerializeObject(this, new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
}

[JsonConverter(typeof(PlatformKeyContactConverter))]
public readonly record struct PlatformKey(string key)
{
    public static PlatformKey Any = new("any");
    public static PlatformKey Windows_x64 = new("win-x64");
    public static PlatformKey Windows_arm64 = new("win-arm64");
    public static PlatformKey Linux_ppc64el = new("linux-ppc64el");
    public static PlatformKey Linux_arm64 = new("linux-arm64");
    public static PlatformKey Linux_x64 = new("linux-x64");
    public static PlatformKey Osx_arm64 = new("osx-arm64");
    public static PlatformKey Osx_x64 = new("osx-x64");

    public static IReadOnlyList<PlatformKey> All = new List<PlatformKey>()
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
            return PlatformKey.Windows_x64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return PlatformKey.Windows_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Ppc64le)
            return PlatformKey.Linux_ppc64el;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return PlatformKey.Linux_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.OSArchitecture == Architecture.X64)
            return PlatformKey.Linux_x64;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            return PlatformKey.Osx_arm64;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.OSArchitecture == Architecture.X64)
            return PlatformKey.Osx_x64;

        throw new PlatformNotSupportedException($"Platform is not supported, only supported '{string.Join(',', All.Select(x => x.key))}'");
    }
}

[JsonConverter(typeof(PackageKeyContactConverter))]
public readonly record struct PackageKey(string key);
[JsonConverter(typeof(WorkloadKeyContactConverter))]
public readonly record struct WorkloadKey(string key);

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
}
