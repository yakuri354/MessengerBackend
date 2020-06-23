using System.IO;

namespace MessengerBackend
{
    public static class Extensions
    {
        public static string GetString(this Stream s)
        {
            using var reader = new StreamReader(s);
            return reader.ReadToEnd();
        }
        
    }
}