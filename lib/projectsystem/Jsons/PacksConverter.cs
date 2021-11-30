namespace vein.project
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NuGet.Versioning;
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    public class PacksConverter : JsonCreationConverter<SDKPack[]>
    {
        protected override SDKPack[] Create(Type objectType, JObject jObject)
        {
            if (!jObject.HasValues)
                return new SDKPack[0];
            var list = new List<SDKPack>();
            foreach (var (key, value) in jObject)
            {
                var result = value.ToObject<SDKPack>();
                result!.Name = key;
                list.Add(result);
            }
            return list.ToArray();
        }
    }

    public class NuGetVersionConverter : Newtonsoft.Json.JsonConverter<NuGetVersion>
    {
        public override void WriteJson(JsonWriter writer, NuGetVersion value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override NuGetVersion ReadJson(JsonReader reader, Type objectType, NuGetVersion existingValue,
            bool hasExistingValue,
            JsonSerializer serializer) => new NuGetVersion((string)reader.Value);
    }
}
