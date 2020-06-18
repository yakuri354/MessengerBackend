using System;
using System.Net.Sockets;
using Serilog;

namespace MessengerBackend.RealTime.Protocol
{
    public class OpenConnection
    {
        public Socket Socket { get; }
        private readonly Guid? UserGuid;
        internal ILogger _logger;
        internal Guid ID => Guid.NewGuid();

        public OpenConnection(Socket s)
        {
            Socket = s;
        }
    }
}