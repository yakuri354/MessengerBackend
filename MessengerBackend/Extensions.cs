using System.IO;
using System.Linq;
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

        public static bool EqualsAnyString(this string self, params string[] args) =>
            args.Any(arg => arg.Equals(self));
    }
}