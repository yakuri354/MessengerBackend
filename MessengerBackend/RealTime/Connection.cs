using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Serilog;

namespace MessengerBackend.RealTime
{
    public class Connection : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        private readonly WebSocket _socket;
        // private readonly TaskCompletionSource<object> _taskCompletionSource;

        private readonly ConcurrentQueue<InboundMessage> _inboundMessages =
            new ConcurrentQueue<InboundMessage>();

        private readonly ConcurrentQueue<OutboundMessage> _outboundMessages =
            new ConcurrentQueue<OutboundMessage>();

        public UserService? UserService;
        public ChatService? ChatService;

        public void SetupDependencies(UserService userService, ChatService chatService)
        {
            UserService = userService;
            ChatService = chatService;
        }

        public bool Authenticated = false;
        public bool Polling;
        public EntityEntry<User>? User;

        public event EventHandler? ConnectionClosed;

        protected virtual void CloseConnection() =>
            ConnectionClosed?.Invoke(this, EventArgs.Empty);

        public Connection(WebSocket socket) => _socket = socket;

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task StartPolling()
        {
            Polling = true;
            await Task.WhenAll(ReceivePollAsync(), SendPollAsync(), Task.Run(ProcessPoll));
            Polling = false;
        }

        private async Task ReceivePollAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var (result, frame) =
                    await _socket.ReceiveFrameAsync(_cancellationTokenSource.Token);
                if (result.CloseStatus.HasValue)
                    CloseConnection();
                try
                {
                    _inboundMessages.Enqueue(MessagePackSerializer.Deserialize<InboundMessage>(frame));
                }
                catch (MessagePackSerializationException e)
                {
                    Log.Warning("Failed to deserialize message from client: " + e.Message);
                }
            }
        }

        private async Task SendPollAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested
                   && _outboundMessages.TryDequeue(out var message))
            {
                try
                {
                    await _socket.SendAsync(new ArraySegment<byte>(MessagePackSerializer.Serialize(message)),
                        WebSocketMessageType.Binary, true, _cancellationTokenSource.Token);
                }
                catch (MessagePackSerializationException e)
                {
                    Log.Error("Outbound serialization error: " + e.Message);
                }
            }
        }

        private void ProcessPoll()
        {
            while (!_cancellationTokenSource.IsCancellationRequested
                   && _inboundMessages.TryDequeue(out var message))
                ProcessAsync(message);
        }

        private async void ProcessAsync(InboundMessage message)
        {
            Console.WriteLine(MessagePackSerializer.SerializeToJson(message));
            _outboundMessages.Enqueue(new OutboundMessage
            {
                Type = OutboundMessageType.Response,
                Data = null,
                ID = message.ID,
                IsSuccess = true
            });
        }
    }
}