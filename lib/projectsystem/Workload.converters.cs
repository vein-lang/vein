namespace vein.project;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        return new((string)reader.Value);
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
            if (jObj.ContainsKey("sdkTarget") || jObj.ContainsKey("SdkTarget"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageSdk>());
                continue;
            }
            if (jObj.ContainsKey("execPath") || jObj.ContainsKey("ExecPath"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageTool>());
                continue;
            }

            if (jObj.ContainsKey("templatePath") || jObj.ContainsKey("TemplatePath"))
            {
                existingValue.Add(jObj.ToObject<WorkloadPackageTemplate>());
                continue;
            }
            if (jObj.ContainsKey("packageTarget") || jObj.ContainsKey("PackageTarget"))
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

public class WorkloadSdkAliasesPackageConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => typeof(Dictionary<PlatformKey, string>).IsAssignableFrom(objectType);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var packages = new Dictionary<PlatformKey, string>();
        var jsonObject = JObject.Load(reader);

        foreach (var property in jsonObject.Properties())
        {
            var package = property.Value.ToString();
            packages[new PlatformKey(property.Name)] = package;
        }

        return packages;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var packages = (Dictionary<PlatformKey, string>)value;
        writer.WriteStartObject();

        foreach (var kvp in packages)
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


public class DictionaryDependencyConverter : JsonConverter<Dictionary<PackageKey, NuGetVersion>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<PackageKey, NuGetVersion> value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key.key);
            serializer.Serialize(writer, kvp.Value.ToNormalizedString());
        }
        writer.WriteEndObject();
    }

    public override Dictionary<PackageKey, NuGetVersion> ReadJson(JsonReader reader, Type objectType,
        Dictionary<PackageKey, NuGetVersion> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var packages = new Dictionary<PackageKey, NuGetVersion>();
        var jsonObject = JObject.Load(reader);

        foreach (var property in jsonObject.Properties())
        {
            var package = property.Value.ToObject<string>(serializer);
            packages[new PackageKey(property.Name)] = NuGetVersion.Parse(package);
        }

        return packages;
    }
}
