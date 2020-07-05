using System.Net.Sockets;
using Serilog;

namespace MessengerBackend.RealTime.Protocol
{
    public class OpenConnection
    {
        public readonly Socket Socket;
        public ILogger Logger;
        public string UserPublicID;

        public OpenConnection(Socket s) => Socket = s;
    }
}