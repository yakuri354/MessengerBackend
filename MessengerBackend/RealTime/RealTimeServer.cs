using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MessengerBackend.RealTime
{
    public sealed class RealTimeServer : IDisposable
    {
        private readonly ConcurrentDictionary<ulong, Connection> _connections =
            new ConcurrentDictionary<ulong, Connection>();

        private readonly CryptoService _cryptoService;
        private readonly ILogger<RealTimeServer> _logger;

        private readonly Random _random = new Random();

        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public RealTimeServer(CryptoService cryptoService, ILogger<RealTimeServer> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public void Dispose()
        {
            _shutdown.Cancel();
            foreach (var connection in _connections) connection.Value.Dispose();
        }

        public async Task Connect(WebSocket sock, MessageProcessService service)
        {
            var connID = (ulong) (_random.NextDouble() * ulong.MaxValue);
            using var conn = new Connection(sock, connID, service);
            _connections[connID] = conn;
            conn.ConnectionClosed += (sender, args) => { _connections.TryRemove(connID, out _); };
            conn.Authorize = async s => await Authorize(s, service.UserService);
            await conn.StartPolling();
        }

        private async Task<User?> Authorize(string token, UserService userService)
        {
            var (securityToken, claimsPrincipal) = _cryptoService.ValidateAccessJWT(token);
            if (!claimsPrincipal.HasClaim("type", "access") || securityToken.ValidTo < DateTime.UtcNow)
                return null;
            var pid = claimsPrincipal.FindFirst("uid").Value;
            return await userService.Users.FirstOrDefaultAsync(user => pid != null && user.UserPID == pid);
        }
    }
}