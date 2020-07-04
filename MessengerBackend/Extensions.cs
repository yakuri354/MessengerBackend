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

        public static string RemoveWhitespace(this string input)
        {
            return input.Replace(" ", "");
        }

        public static byte[] ToByteArray(this DerAsnBitString bitString)
        {
            return bitString.Encode(null).Skip(1).ToArray();
        }

        public static T DeserializeAnonymousType<T>(string json, T obj)
        {
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}