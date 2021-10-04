namespace vein.project
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

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
}
