using System;
using System.Net.Sockets;
using Serilog;

namespace MessengerBackend.RealTime.Protocol
{
    public class OpenConnection
    {
        public ILogger Logger; 
        public string UserPublicID;

        public OpenConnection(Socket s)
        {
            Socket = s;
        }

        public readonly Socket Socket;
    }
}