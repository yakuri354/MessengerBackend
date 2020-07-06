using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;

namespace MessengerBackend.Utils
{
    public static class MyJsonDeserializer
    {
        public static T DeserializeAnonymousType<T>(string json, T target, bool nullable = false)
        {
            var parsedJson = JsonConvert.DeserializeObject<T>(json);
            if (nullable) return parsedJson;
            if (parsedJson == null) throw new JsonException("no input provided");
            foreach (var propertyInfo in typeof(T).GetProperties())
                if (propertyInfo.GetValue(parsedJson) == null)
                    throw new JsonException($"field '{propertyInfo.Name}' is null");
            return parsedJson;
        }
    }
}