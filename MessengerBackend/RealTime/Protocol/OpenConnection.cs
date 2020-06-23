using System;
using System.Net.Sockets;
using Serilog;

namespace MessengerBackend.RealTime.Protocol
{
    public class OpenConnection
    {
        public Socket Socket { get; }
        internal ILogger _logger;
        internal Guid ID;

        public OpenConnection(Socket s)
        {
            Socket = s;
        }
    }
}