using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessengerBackend.Services;
using Microsoft.Extensions.Logging;

namespace MessengerBackend.RealTime
{
    public sealed class RealTimeServer : IDisposable
    {
        private readonly CryptoService _cryptoService;
        private readonly ILogger<RealTimeServer> _logger;

        private readonly Random _random = new Random();

        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public readonly ConcurrentDictionary<ulong, Connection> Connections =
            new ConcurrentDictionary<ulong, Connection>();

        public RealTimeServer(CryptoService cryptoService, ILogger<RealTimeServer> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public void Dispose()
        {
            _shutdown.Cancel();
            foreach (var connection in Connections)
            {
                connection.Value.Dispose();
            }
        }

        public async Task Connect(WebSocket sock, MessageProcessService service)
        {
            var connID = (ulong) (_random.NextDouble() * ulong.MaxValue);
            using var conn = new Connection(sock, connID, service, _cryptoService);
            Connections[connID] = conn;
            conn.ConnectionClosed += (sender, args) => { Connections.TryRemove(connID, out _); };
            await conn.StartPolling();
        }
    }
}