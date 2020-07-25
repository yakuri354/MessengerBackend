using MessengerBackend.Errors;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;
using NewtonsoftJsonException = Newtonsoft.Json.JsonException;

namespace MessengerBackend.Utils
{
    public static class MyJsonDeserializer
    {
        public static T DeserializeAnonymousType<T>(string json, T target, bool nullable = false)
        {
            try
            {
                var parsedJson = JsonConvert.DeserializeObject<T>(json);
                if (nullable) return parsedJson;
                if (parsedJson == null) throw new JsonParseException("Json was required, but not provided");
                foreach (var propertyInfo in typeof(T).GetProperties())
                    if (propertyInfo.GetValue(parsedJson) == null)
                        throw new JsonParseException($"Field '{propertyInfo.Name}' must be provided");
                return parsedJson;
            }
            catch (JsonException e)
            {
                throw new JsonParseException(e);
            }
            catch (NewtonsoftJsonException e)
            {
                throw new JsonParseException(e);
            }
        }
    }
}