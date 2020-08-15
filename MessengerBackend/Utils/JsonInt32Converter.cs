using System;
using Newtonsoft.Json;

namespace MessengerBackend.Utils
{
    /// <summary>
    ///     To address issues with automatic Int64 deserialization -- see https://stackoverflow.com/a/9444519/1037948
    /// </summary>
    public class JsonInt32Converter : JsonConverter
    {
        #region Overrides of JsonConverter

        /// <summary>
        ///     Only want to deserialize
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        ///     Placeholder for inheritance -- not called because <see cref="CanWrite" /> returns false
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // since CanWrite returns false, we don't need to implement this
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        ///     The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) =>
            reader.TokenType == JsonToken.Integer
                ? Convert.ToInt32(reader.Value) // convert to Int32 instead of Int64
                : serializer.Deserialize(reader); // default to regular deserialization

        /// <summary>
        ///     Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        ///     <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(int) ||
            objectType == typeof(long) ||
            // need this last one in case we "weren't given" the type
            // and this will be accounted for by `ReadJson` checking token type
            objectType == typeof(object);

        #endregion
    }


    // public class JsonInt32Converter : JsonConverter
    // {
    //     public override bool CanWrite =>
    //         // we only want to read (de-serialize)
    //         false;
    //
    //     public override bool CanConvert(Type objectType) =>
    //         // may want to be less concrete here
    //         objectType == typeof(Dictionary<string, object>);
    //
    //     public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
    //         JsonSerializer serializer)
    //     {
    //         // again, very concrete
    //         Dictionary<string, object> result = new Dictionary<string, object>();
    //         reader.Read();
    //
    //         while (reader.TokenType == JsonToken.PropertyName)
    //         {
    //             string propertyName = reader.Value as string;
    //             reader.Read();
    //
    //             object value;
    //             value = reader.TokenType == JsonToken.Integer ? Convert.ToInt32(reader.Value) : serializer.Deserialize(reader);
    //             result.Add(propertyName, value);
    //             reader.Read();
    //         }
    //
    //         return result;
    //     }
    //
    //     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //     {
    //         // since CanWrite returns false, we don't need to implement this
    //         throw new NotImplementedException();
    //     }
    // }
}