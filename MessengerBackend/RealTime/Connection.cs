using System;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Serilog;


// TODO Optimize reflection

namespace MessengerBackend.RealTime
{
    public class Connection : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();
        // private readonly TaskCompletionSource<object> _taskCompletionSource;

        private readonly Channel<InboundMessage> _inboundMessages =
            Channel.CreateBounded<InboundMessage>(10000);

        private readonly Channel<OutboundMessage> _outboundMessages =
            Channel.CreateBounded<OutboundMessage>(10000);

        public ulong ConnectionID;
        private SerializationType _serializationType;
        private bool _connectReceived;

        private enum SerializationType
        {
            Json, // for debug
            MessagePack // for production
        }

        private readonly WebSocket _socket;

        public Func<string, Task<User?>>? Authorize;

        public User? User;
        private readonly MessageProcessService _messageProcessService;

        public Connection(WebSocket socket, ulong connectionID, MessageProcessService messageProcessService)
        {
            _socket = socket;
            ConnectionID = connectionID;
            _messageProcessService = messageProcessService;
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

        private void CloseConnection() =>
            ConnectionClosed?.Invoke(this, EventArgs.Empty);

        public Task StartPolling() =>
            Task.WhenAll(ReceivePollAsync(), SendPollAsync(), ProcessPollAsync());


        private async Task ReceivePollAsync()
        {
            var writer = _inboundMessages.Writer;
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var (result, frame) =
                    await _socket.ReceiveFrameAsync(_cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    CloseConnection();
                    continue;
                }

                if (!_connectReceived)
                {
                    _serializationType = result.MessageType switch
                    {
                        WebSocketMessageType.Binary => SerializationType.MessagePack,
                        WebSocketMessageType.Text => SerializationType.Json,
                        _ => throw new NotImplementedException()
                    };
                }

                var message = Deserialize(frame);
                if (message == null) continue;
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
            var reader = _outboundMessages.Reader;
            await foreach (var message in reader.ReadAllAsync(_cancellationTokenSource.Token))
                try
                {
                    var msg = Serialize(message);
                    if (msg == null) continue;
                    await _socket.SendAsync(new ArraySegment<byte>(msg),
                        _serializationType switch
                        {
                            SerializationType.Json => WebSocketMessageType.Text,
                            SerializationType.MessagePack => WebSocketMessageType.Binary,
                            _ => throw new NotImplementedException()
                        }, true, _cancellationTokenSource.Token);
                }
                catch (MessagePackSerializationException e)
                {
                    Log.Error("Outbound serialization error: " + e.Message);
                }
        }

        private async Task ProcessPollAsync()
        {
            var reader = _inboundMessages.Reader;
            var writer = _outboundMessages.Writer;
            await foreach (var message in reader.ReadAllAsync())
            {
                if (!_connectReceived && message.Type != InboundMessageType.Connect) continue;
                if (!await VerifyAsync(message)) continue;
                // await writer.WriteAsync(new OutboundMessage
                // {
                //     ID = 1,
                //     Type = OutboundMessageType.Response,
                //     IsSuccess = true
                // });
                switch (message.Type)
                {
                    case InboundMessageType.Method:
                        var method = GetMethod(message.Method);
                        try
                        {
                            OutboundMessage reply;
                            if (method?.ReturnType.GetMethod("GetAwaiter") != null)
                            {
                                reply = await (Task<OutboundMessage>)
                                    method?
                                        .Invoke(_messageProcessService, message.Params!.ToArray())!;
                            }
                            else
                            {
                                reply = (OutboundMessage)
                                    method?
                                        .Invoke(_messageProcessService, message.Params!.ToArray())!;
                            }

                            reply.ID = message.ID;
                            await writer.WriteAsync(reply);
                        }
                        catch (ProcessException ex)
                        {
                            await Fail(ex.Message, message.ID);
                        }

                        break;
                    case InboundMessageType.Subscribe:
                        await _messageProcessService.ChatService.Subscribe((string) message!.Params![0], User!);
                        break;
                    case InboundMessageType.Unsubscribe:
                        await NotImplemented(message.ID);
                        break;
                    case InboundMessageType.Connect:
                        _serializationType = (SerializationType) Convert.ToInt32(message.Params![0]);
                        _connectReceived = true;
                        await Success(message.ID);
                        break;
                    case InboundMessageType.Auth:
                        User = await Authorize?.Invoke((string) message.Params![0])!;
                        if (User == null)
                            await Fail("Authorization failed", message.ID);
                        else await Success(message.ID);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task<bool> VerifyAsync(InboundMessage message)
        {
            switch (message.Type)
            {
                case InboundMessageType.Method:
                    var method = GetMethod(message.Method);
                    if (method == null)
                        // || !(method.ReturnParameter.ParameterType == typeof(OutboundMessage)))
                    {
                        await Fail("No such method: " + message.Method, message.ID);
                        return false;
                    }

                    var isAuthorized = method.GetCustomAttribute<AuthorizeAttribute>() != null;
                    if (isAuthorized && User == null)
                    {
                        await Fail($"Method {message.Method} requires authorization", message.ID);
                        return false;
                    }
                    else if (isAuthorized && User != null)
                        _messageProcessService.Caller = User;

                    var parameters = method.GetParameters();
                    if (parameters == null || parameters.Length == 0) return true;
                    var args = parameters.Count(p => !p.IsOptional);
                    if (message.Params == null ||
                        parameters.Count(p => !p.IsOptional) != message.Params?.Count)
                    {
                        await Fail(
                            $"Method {message.Method} argument count mismatch, expected {args}," +
                            $" got {message.Params?.Count ?? 0}",
                            message.ID);
                        return false;
                    }

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.IsOptional)
                            continue;
                        var paramType = param.ParameterType;
                        var actualType = message.Params?[i].GetType();
                        if (paramType == actualType) continue;
                        await Fail($"Expected type {paramType.Name} for argument {i}," +
                                   $" instead got {actualType?.Name ?? "unknown"}", message.ID);
                        return false;
                    }

                    break;
                case InboundMessageType.Subscribe:
                    if (User == null)
                    {
                        await Fail("Authorization required for subscriptions", message.ID);
                        return false;
                    }

                    var parametersCount = message.Params?.Count ?? 0;
                    if (!((parametersCount == 1 || parametersCount == 2) // either 1 or 2 params
                          && message.Params?[0] is string) // first param is a string (channel name)
                        && (parametersCount == 1 || message.Params?[1] is IEnumerable<object?>
                            // second param is a list of channel parameters or null if there is only one parameter
                        )
                    )
                    {
                        await Fail("To subscribe you must provide a channel name " +
                                   "and may provide optional parameters",
                            message.ID);
                        return false;
                    }

                    // TODO Verify Subscription
                    
                    break;
                case InboundMessageType.Unsubscribe:
                    if (User == null)
                    {
                        await Fail("Authorization required for unsubscription", message.ID);
                        return false;
                    }

                    //TODO Unsubscribe
                    break;
                case InboundMessageType.Connect:
                    if (message.Params?[0] == null ||
                        !Enum.IsDefined(typeof(SerializationType), Convert.ToInt32(message.Params[0])))
                        return false;
                    break;
                case InboundMessageType.Auth:
                    if (!((message.Params?.Count ?? 0) == 1 && message.Params?[0] is string))
                        await Fail("Access token (param 1, string) is missing", message.ID);
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        private static MethodInfo? GetMethod(string? name) =>
            name == null
                ? null
                : typeof(MessageProcessService)
                    .GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        private ValueTask Fail(string error, uint id) => _outboundMessages.Writer.WriteAsync(new OutboundMessage
        {
            Data = new Dictionary<string, object> { { "error", error } },
            IsSuccess = false,
            ID = id,
            Type = OutboundMessageType.Response
        });

        private ValueTask NotImplemented(uint id) => Fail("Not implemented", id);

        private ValueTask Success(uint id) => _outboundMessages.Writer.WriteAsync(new OutboundMessage
        {
            IsSuccess = true,
            ID = id,
            Type = OutboundMessageType.Response
        });

        private InboundMessage? Deserialize(byte[] message)
        {
            try
            {
                return _serializationType switch
                {
                    SerializationType.MessagePack => MessagePackSerializer.Deserialize<InboundMessage>(message),
                    SerializationType.Json => JsonConvert.DeserializeObject<InboundMessage>(
                        Encoding.UTF8.GetString(message)),
                    _ => throw new NotImplementedException()
                };
            }
            catch (Exception e)
                when (e is JsonException || e is MessagePackSerializationException)
            {
                Fail($"Deserialization error: {e.Message}", 0);
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
                    _ => throw new NotImplementedException()
                };
            }
            catch (Exception e) when (e is JsonException || e is MessagePackSerializationException)
            {
                Log.Error("Serialization error");
                Log.Debug("Message: {message}", message);
                Fail("Server Error", message.ID);
                return null;
            }
        }
    }
}