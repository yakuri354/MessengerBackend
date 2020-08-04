using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessengerBackend.Services;

namespace MessengerBackend.RealTime
{
    public sealed class RealTimeServer : IDisposable
    {
        private readonly ConcurrentDictionary<ulong, Connection> _connections =
            new ConcurrentDictionary<ulong, Connection>();

        private readonly Random _random = new Random();

        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public void Dispose()
        {
            _shutdown.Cancel();
            foreach (var connection in _connections) connection.Value.Dispose();
        }

        public async Task Connect(WebSocket sock, UserService userService, ChatService chatService)
        {
            using var conn = new Connection(sock);
            conn.SetupDependencies(userService, chatService);
            var connID = (ulong) (_random.NextDouble() * ulong.MaxValue);
            _connections[connID] = conn;
            conn.ConnectionClosed += (sender, args) => { _connections.TryRemove(connID, out _); };
            await conn.StartPolling();
        }
    }
}