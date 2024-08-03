namespace vein.json;

using System;
using System.IO;
using Newtonsoft.Json;

[ExcludeFromCodeCoverage]
public class FileInfoSerializer : JsonConverter<FileInfo>
{
    public override void WriteJson(JsonWriter writer, FileInfo value, JsonSerializer serializer)
        => writer.WriteValue(value.FullName);

    public override FileInfo ReadJson(JsonReader reader, Type objectType, FileInfo existingValue, bool hasExistingValue,
        JsonSerializer serializer) =>
        new FileInfo((string)reader.Value);
}
