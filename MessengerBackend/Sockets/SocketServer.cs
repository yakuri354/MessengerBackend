using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using System.IO;
using MessengerBackend.Sockets.Models;


namespace MessengerBackend.Sockets
{

    // Envelope for all messages handled by the server

    


    // The socket server, using DotNetty's SimpleChannelInboundHandler
    // The ChannelRead0 method is called for each Message received
    public class SocketServer : SimpleChannelInboundHandler<RawSocketMessage>, IDisposable
    {
        private readonly ConcurrentDictionary<string, SocketClient> _sclients;
        private readonly ConcurrentQueue<UnhandledRawSocketMessage> _unhandledMessages;
        private readonly CancellationTokenSource _cancellation;
        private readonly AutoResetEvent _newMessage;

        public SocketServer(CancellationToken cancellation)
        {
            _sclients = new ConcurrentDictionary<string, SocketClient>();
            _newMessage = new AutoResetEvent(false);
            this._cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        }

        // The start method should be called when the server is bound to a port.
        // Messages will be received, but will not be processed unless/until the Start method is called
        public Task Start()
        {
            // Start a dedicated thread to process messages so that the ChannelRead operation does not block
            return Task.Run(() =>
            {
                var serializer =
                    JsonSerializer.CreateDefault(); // This will be used to deserialize the Data member of the messages

                while (!_cancellation.IsCancellationRequested)
                {
                    _newMessage.WaitOne(50); // Sleep until a new message arrives

                    while (_unhandledMessages.TryDequeue(out var socketMessage))
                        // Process each message in the queue, then sleep until new messages arrive
                    {
                        if (socketMessage != null)
                        {
                            Task.Run(() =>
                            {
                                ProcessMessage(socketMessage);
                            });
                        }
                    }
                }
            }, _cancellation.Token);
        }

        private void ProcessMessage(UnhandledRawSocketMessage socketMessage)
        {
            // switch (socketMessage.Command)
            // {
            //     case Command.Register:
            //         // Register a new client, or update an existing client with a new Context
            //         var sclient = new SocketClient(socketMessage.ClientId, socketMessage.Context);
            //         _sclients.AddOrUpdate(socketMessage.ClientId, sclient, (_, __) => sclient);
            //         break;
            //     case Command.SendToClient:
            //         SocketClient destinationClient;
            //         using (var reader = new JsonTextReader(new StringReader(socketMessage.Data)))
            //         {
            //             var sendToClientCommand = serializer.Deserialize<SendToClientCommand>(reader);
            //             if (_sclients.TryGetValue(sendToClientCommand.DestinationClientId,
            //                 out destinationClient))
            //             {
            //                 var clientMessage = new SocketMessage
            //                 {
            //                     ClientId = socketMessage.ClientId,
            //                     Command = sendToClientCommand.ClientCommand,
            //                     Data = sendToClientCommand.Data
            //                 };
            //                 destinationClient.Context.Channel.WriteAndFlushAsync(clientMessage);
            //             }
            //         }
            //
            //         break;
            // }
        }

        // Receive each message from the clients and enqueue them to be procesed by the dedicated thread
        protected override void ChannelRead0(IChannelHandlerContext context, RawSocketMessage socketMessage)
        {
            _unhandledMessages.Enqueue(new UnhandledRawSocketMessage(socketMessage, context));
            _newMessage.Set(); // Trigger an event so that the thread processing messages wakes up when a new message arrives
        }

        // Flush the channel once the Read operation has completed
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
            base.ChannelReadComplete(context);
        }

        // Automatically stop the message-processing thread when this object is disposed
        public void Dispose()
        {
            _cancellation.Cancel();
        }
    }
}