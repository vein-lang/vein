namespace wave.project
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public abstract class JsonCreationConverter<T> : JsonConverter
    {
        protected abstract T Create(Type objectType, JObject jObject);

        public override bool CanConvert(Type objectType) 
            => typeof(T).IsAssignableFrom(objectType);

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) 
            => throw new NotImplementedException();

        public override object ReadJson(JsonReader r, Type o, object _, JsonSerializer s)
        {
            var jObject = JObject.Load(r);
            var target = Create(o, jObject);
            return target;
        }
    }
}