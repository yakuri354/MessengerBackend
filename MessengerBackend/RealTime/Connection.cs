using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MessagePack;
using MessengerBackend.Models;
using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace MessengerBackend.RealTime
{
    public class Connection : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        private readonly ulong _connectionID;
        private readonly CryptoService _cryptoService;

        private readonly ConcurrentDictionary<string, MethodDelegate> _delegates =
            new ConcurrentDictionary<string, MethodDelegate>();
        // private readonly TaskCompletionSource<object> _taskCompletionSource;

        private readonly Channel<InboundMessage> _inboundMessages =
            Channel.CreateBounded<InboundMessage>(10000);

        private readonly ILogger _logger;
        private readonly MessageProcessService _messageProcessService;

        private readonly WebSocket _socket;

        public readonly ConcurrentDictionary<ulong, Func<InboundMessage, Task>> AnswerPendingMessages =
            new ConcurrentDictionary<ulong, Func<InboundMessage, Task>>();

        public readonly Channel<OutboundMessage> OutboundMessages =
            Channel.CreateBounded<OutboundMessage>(10000);

        private bool _connectReceived;
        private SerializationType _serializationType;

        public User? User;

        public Connection(WebSocket socket, ulong connectionID, MessageProcessService messageProcessService,
            CryptoService cryptoService)
        {
            _socket = socket;
            _connectionID = connectionID;
            _messageProcessService = messageProcessService;
            _logger = new LoggerConfiguration()
                .MinimumLevel.Is(Program.LogEventLevel)
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.WithDynamicProperty("UserID", () => User?.UserID.ToString() ?? "<null>")
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [CONN {ConnectionID}] [MSG {MessageID}] [USR {UserID}]" +
                    " {Message}{NewLine}{Exception}")
                .CreateLogger()
                .ForContext<Connection>()
                .ForContext("ConnectionID", _connectionID)
                .ForContext("MessageID", "<null>");
            _cryptoService = cryptoService;
            SetupReflection();
            _logger.Information("New connection established");
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        // public void SetupDependencies(UserService userService, ChatService chatService)
        // {
        //     UserService = userService;
        //     ChatService = chatService;
        // }

        public event EventHandler? ConnectionClosed;

        private void CloseConnection()
        {
            _logger.Information("Closed connection");
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        public async Task StartPolling()
        {
            // using var _ = LogContext.PushProperty("Connection", this);
            _logger.Debug("Started polling", _connectionID);
            await Task.WhenAll(ReceivePollAsync(), SendPollAsync(), ProcessPollAsync());
        }


        private async Task ReceivePollAsync()
        {
            var writer = _inboundMessages.Writer;
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var (result, frame) =
                    await _socket.ReceiveFrameAsync(_cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.Debug("Close received", _connectionID);
                    CloseConnection();
                    continue;
                }

                if (!_connectReceived)
                {
                    _serializationType = result.MessageType switch
                    {
                        WebSocketMessageType.Binary => SerializationType.MessagePack,
                        WebSocketMessageType.Text => SerializationType.Json,
                        WebSocketMessageType.Close => throw new InvalidEnumArgumentException(),
                        _ => throw new InvalidEnumArgumentException()
                    };
                }

                var message = Deserialize(frame);
                if (message == null)
                {
                    continue;
                }

                await writer.WriteAsync(message,
                    _cancellationTokenSource.Token);
                // await writer.WriteAsync(new InboundMessage
                // {
                //     ID = 1, Type = InboundMessageType.Method
                // }, _cancellationTokenSource.Token);
            }
        }

        private async Task SendPollAsync()
        {
            var reader = OutboundMessages.Reader;
            await foreach (var message in reader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                try
                {
                    var msg = Serialize(message);
                    if (msg == null)
                    {
                        continue;
                    }

                    _logger.Debug("Sending message");
                    if (_socket.State == WebSocketState.Open)
                    {
                        await _socket.SendAsync(new ArraySegment<byte>(msg),
                            _serializationType switch
                            {
                                SerializationType.Json => WebSocketMessageType.Text,
                                SerializationType.MessagePack => WebSocketMessageType.Binary,
                                _ => throw new InvalidEnumArgumentException()
                            }, true, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        CloseConnection();
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e,
                        "Exception occurred while serializing/sending message",
                        _connectionID);
                    _logger.Debug("Message with error:{NewLine}{@Message}", message);
                }
            }
        }

        private async Task ProcessPollAsync()
        {
            var reader = _inboundMessages.Reader;
            var writer = OutboundMessages.Writer;
            await foreach (var message in reader.ReadAllAsync())
            {
                var logger = _logger.ForContext("MessageID", message.ID);
                logger.Debug("Received new message");
                if (!_connectReceived && message.Type != InboundMessageType.Connect)
                {
                    logger.Information("Message discarded before 'Connect'");
                    continue;
                }

                if (message.Type == InboundMessageType.Response &&
                    AnswerPendingMessages.TryRemove(message.ID, out var func))
                {
                    logger.Information("Found message in pending, invoking handler");
                    await func(message);
                    continue;
                }

                try
                {
                    await writer.WriteAsync(message.Type switch
                    {
                        InboundMessageType.Method => await CallMethod(message, logger),
                        InboundMessageType.Subscribe => await Subscribe(message, logger),
                        InboundMessageType.Unsubscribe => await Unsubscribe(message, logger),
                        InboundMessageType.Connect => Connect(message, logger),
                        InboundMessageType.Auth => await Authenticate(message, logger),
                        _ => Fail("This message type is not supported", message.ID)
                    });
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error in processing message:");
                }
            }
        }

        private OutboundMessage Connect(InboundMessage message, ILogger logger)
        {
            var vResult = Verify(message, b =>
                b.Argument<long>(arg =>
                    Enum.IsDefined(typeof(SerializationType), Convert.ToInt32(arg)) ? null : ""), logger);
            if (vResult != null)
            {
                return Fail(vResult, message.ID);
            }

            _serializationType = (SerializationType) Convert.ToInt32(message.Params![0]);
            _connectReceived = true;
            logger.Information("Serialization type {SerializationType} established",
                _serializationType.ToString());
            return Success(message.ID);
        }

        private async Task<OutboundMessage> Authenticate(InboundMessage message, ILogger logger)
        {
            logger.Debug("Starting authentication");
            var vResult = Verify(message, b => b.Argument<string>(), logger);
            if (vResult != null)
            {
                return Fail(vResult, message.ID);
            }

            var (_, claimsPrincipal) = _cryptoService.ValidateAccessJWT((string) message.Params![0]);
            if (!claimsPrincipal.HasClaim("type", "access"))
            {
                logger.Information("Authentication failed: invalid token");
                return Fail("Wrong token", message.ID);
            }

            var pid = claimsPrincipal.FindFirst("uid").Value;
            if (pid == null)
            {
                logger.Information("Authentication failed: invalid token");
                return Fail("Wrong token", message.ID);
            }

            User = await _messageProcessService.UserService.Users
                .SingleOrDefaultAsync(user => user.UserPID == pid);
            if (User == null)
            {
                logger.Information("Authentication failed");
                return Fail("Authorization failed", message.ID);
            }

            logger.Information("Authentication succeeded");
            return Success(message.ID);
        }

        private async Task<OutboundMessage> CallMethod(InboundMessage message, ILogger logger)
        {
            if (message.Method == null || !_delegates.ContainsKey(message.Method))
                // || !(method.ReturnParameter.ParameterType == typeof(OutboundMessage)))
            {
                logger.Information("Method {Method} not found", message.Method);
                return Fail("No such method: " + message.Method, message.ID);
            }

            logger.Information("Started {Method} invocation", message.Method);

            var method = _delegates[message.Method];

            if (method.Authenticated && User == null)
            {
                logger.Information("Method {Method} unauthorized", message.Method);
                return Fail($"Method {message.Method} requires authorization", message.ID);
            }

            var vResult = Verify(message, b => b.Method(method.MethodInfo), logger);
            if (vResult != null)
            {
                return Fail(vResult, message.ID);
            }

            _messageProcessService.Current = this;
            _messageProcessService.CurrentMessageID = message.ID;
            var reply = await method.InvokeAsync(message.Params!.ToArray());
            logger.Debug("Method {Method} invocation finished", message.Method);
            reply.ID = message.ID;
            return reply;
        }

        private void SetupReflection()
        {
            var type = typeof(MessageProcessService);
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                _delegates[method.Name] = new MethodDelegate(_messageProcessService, type, method,
                    method.GetCustomAttribute<AuthorizeAttribute>() != null);
            }

            _logger.Verbose("Reflection set up successfully");
        }

        private async Task<OutboundMessage> Subscribe(InboundMessage message, ILogger logger)
        {
            logger.Debug("Subscribing");
            if (User == null)
            {
                logger.Information("Subscription unauthorized", message.Method);
                return Fail("Authorization required for subscriptions", message.ID);
            }

            // var parametersCount = message.Params?.Count ?? 0;
            // if (!(message.Params != null && // params are not null
            //       (parametersCount == 1 || parametersCount == 2) // either 1 or 2 params
            //       && message.Params?[0] is string) // first param is a string (channel name)
            //     && (parametersCount == 1 || message.Params?[1] is IEnumerable<object?>
            //         // second param is a list of channel parameters or null if there is only one parameter
            //     )
            // )
            var vResult = Verify(message, b => b
                .Argument<string>()
                .Argument<ICollection<object>>(null, false), logger);
            if (vResult != null)
            {
                return Fail($"{vResult}; To subscribe you must provide a channel name " +
                            "and may provide optional parameters",
                    message.ID);
            }

            var channel = (string) message.Params![0];
            var addr = channel.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var enumParseSuccess = Enum.TryParse(typeof(SubscriptionType), addr[1],
                true, out var subType);
            if (!(addr.Length >= 2 && enumParseSuccess)
            )
            {
                logger.Information(
                    "Subscription address {Address} invalid", channel);
                return Fail($"Invalid subscription address: {message.Params![0] as string ?? "<unknown>"}",
                    message.ID);
            }

            var room = await _messageProcessService.ChatService.Rooms.Include(
                    r => r.Participants)
                .SingleOrDefaultAsync(r => r.RoomPID == addr[0]);
            if (!(room?.Users?.Any(u => u.UserID == User.UserID) ?? false))
            {
                logger.Information("User {UserID} not found in room {RoomID} when subscribing to {Channel}",
                    User.UserID, room?.RoomID.ToString() ?? "<unknown>");
                return Fail("Access Denied",
                    message.ID);
            }

            if (_messageProcessService.ChatService.Subscriptions
                .Include(s => s.User)
                .Any(s => s.Room.RoomID == room.RoomID && s.User.UserID == User.UserID))
            {
                logger.Information("Subscription on channel {Channel} already exists", channel);
                return Fail("Subscription already exists",
                    message.ID);
            }

            var sub = await _messageProcessService.ChatService
                .Subscribe(new Subscription
                {
                    Room = room!,
                    Type = (SubscriptionType) subType!,
                    User = User
                });
            logger.Information(
                "Successfully subscribed to {Channel}", (string) message.Params[0]);
            return Success(new Dictionary<string, object?>
            {
                { "channel", $"/{sub.SubscriptionPID}/{sub.Type.ToString()}" },
                { "subscriptionPID", sub.SubscriptionPID }
            }, message.ID);
        }

        private async Task<OutboundMessage> Unsubscribe(InboundMessage message, ILogger logger)
        {
            if (User == null)
            {
                logger.Information("Unsubscription unauthorized");
                return Fail("Authorization required for unsubscription", message.ID);
            }

            var vResult = Verify(message, b => b.Argument<string>(), logger);
            if (vResult != null)
            {
                return Fail(vResult, message.ID);
            }

            var sub = await
                _messageProcessService.ChatService.Unsubscribe((string) message!.Params![0], User);
            if (sub == null)
            {
                logger.Information("Failed to unsubscribe", message.Method);
                return Fail("No such subscription", message.ID);
            }

            logger.Information("Successfully removed subscription {SubscriptionID}",
                sub.SubscriptionPID);
            return Success(new Dictionary<string, object?>
            {
                { "subscriptionPID", sub.SubscriptionPID }
            }, message.ID);
        }

        private static string? Verify(InboundMessage message, Func<VerificationBuilder, VerificationBuilder> predicate,
            ILogger logger)
        {
            logger.Verbose("Starting verification");
            var builder = new VerificationBuilder(message, logger);
            predicate(builder);
            return builder.Build();
        }

        private OutboundMessage Fail(string error, uint id)
        {
            _logger.Debug(
                "Sending Fail answer to message ID {MessageID}, Error {Error}",
                id, error ?? "<unknown>");
            return new OutboundMessage
            {
                Data = new Dictionary<string, object?> { { "error", error } },
                IsSuccess = false,
                ID = id,
                Type = OutboundMessageType.Response
            };
        }

        // private OutboundMessage NotImplemented(uint id) => Fail("Not implemented", id);
        private OutboundMessage Success(uint id)
        {
            _logger.ForContext("MessageID", id).Debug("Success");
            return new OutboundMessage
            {
                IsSuccess = true,
                ID = id,
                Type = OutboundMessageType.Response
            };
        }

        private OutboundMessage Success(Dictionary<string, object?>? data, uint id)
        {
            _logger.ForContext("MessageID", id).Debug("Success with data");
            return new OutboundMessage
            {
                IsSuccess = true,
                ID = id,
                Data = data,
                Type = OutboundMessageType.Response
            };
        }

        private InboundMessage? Deserialize(byte[] message)
        {
            try
            {
                var serialized = _serializationType switch
                {
                    SerializationType.MessagePack => MessagePackSerializer.Deserialize<InboundMessage>(message),
                    SerializationType.Json => JsonConvert.DeserializeObject<InboundMessage>(
                        Encoding.UTF8.GetString(message)),
                    _ => throw new InvalidEnumArgumentException()
                };
                return serialized;
            }
            catch (Exception e)
                when (e is JsonException || e is MessagePackSerializationException)
            {
                _logger.Information(e,
                    "Deserialization failed for connection ID {ConnectionID}", _connectionID);
                return null;
            }
        }

        private byte[]? Serialize(OutboundMessage message)
        {
            try
            {
                return _serializationType switch
                {
                    SerializationType.MessagePack => MessagePackSerializer.Serialize(message),
                    SerializationType.Json => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)),
                    _ => throw new InvalidEnumArgumentException()
                };
            }
            catch (Exception e) when (e is JsonException || e is MessagePackSerializationException)
            {
                _logger.Error(e, "Serialization error for outbound message ID {MessageID}");
                return null;
            }
        }

        private enum SerializationType
        {
            Json, // for debug
            MessagePack // for production
        }

        private class MethodDelegate : EfficientReflectionDelegate<OutboundMessage>
        {
            private readonly MessageProcessService _mpc;
            public readonly bool Authenticated;

            public MethodDelegate(
                MessageProcessService mpc,
                Type type,
                MethodInfo methodInfo,
                bool authenticated) : base(type, methodInfo, authenticated)
            {
                _mpc = mpc;
                Authenticated = authenticated;
            }

            public Task<OutboundMessage> InvokeAsync(object[] parameters) =>
                Invoke(_mpc, parameters);
        }
    }
}