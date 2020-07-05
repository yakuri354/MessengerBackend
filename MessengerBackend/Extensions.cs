using System.IO;
using System.Linq;
using System.Text.Json;
using DerConverter.Asn.KnownTypes;

namespace MessengerBackend
{
    internal static class Extensions
    {
        public static string GetString(this Stream s)
        {
            using var reader = new StreamReader(s);
            return reader.ReadToEnd();
        }

        public static string RemoveWhitespace(this string input) => input.Replace(" ", "");

        public static byte[] ToByteArray(this DerAsnBitString bitString) => bitString.Encode(null).Skip(1).ToArray();

        public static T DeserializeAnonymousType<T>(string json, T obj) => JsonSerializer.Deserialize<T>(json);
    }
}