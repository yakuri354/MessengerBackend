using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessengerBackend.Sockets
{
    public static class ThreadDispatcher
    {
        private static Task _socketListener;
        private static SocketServer _socketServer;

        public static void RegisterSocketListener(CancellationTokenSource c)
        {
            if (_socketListener != null) return;
            _socketServer = new SocketServer(c.Token);
            _socketListener = _socketServer.Start();
        }
    }
}