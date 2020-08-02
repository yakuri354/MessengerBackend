using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessengerBackend.Services;
using MessengerBackend.Utils;

namespace MessengerBackend.RealTime
{
    public class RealTimeServer : IDisposable
    {
        private readonly UserService _userService;
        private ConcurrentBag<OpenConnection> _connections;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public RealTimeServer(UserService userService) => _userService = userService;

        public async void ProcessNewConnectionAsync(WebSocket sock, TaskCompletionSource<object> tcs)
        {
            var buf = new byte[1024 * 4];
            var result = await sock.ReceiveAsync(new ArraySegment<byte>(buf), _shutdown.Token);
            if (result.MessageType != WebSocketMessageType.Text)
            {
                await sock.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                    "First request must be auth data in form of JSON", _shutdown.Token);
            }
            // TODO
        }

        public void Dispose()
        {
            _shutdown.Cancel();
        }
    }
}