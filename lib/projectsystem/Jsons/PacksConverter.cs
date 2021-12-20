namespace vein.project
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NuGet.Versioning;
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    public class NuGetVersionConverter : Newtonsoft.Json.JsonConverter<NuGetVersion>
    {
        public override void WriteJson(JsonWriter writer, NuGetVersion value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override NuGetVersion ReadJson(JsonReader reader, Type objectType, NuGetVersion existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) => new NuGetVersion((string)reader.Value);
    }
}
