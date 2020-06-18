// using System;
// using System.Collections.Concurrent;
// using System.Threading;
// using System.Threading.Tasks;
// using DotNetty.Transport.Channels;
// using MessengerBackend.RealTime.Protocol;
//
//
// namespace MessengerBackend.RealTime
// {
//     // The socket server, using DotNetty's SimpleChannelInboundHandler
//     // The ChannelRead0 method is called for each Message received
//     public class SocketServer : SimpleChannelInboundHandler<byte[]>, IDisposable
//     {
//         private readonly ConcurrentDictionary<string, RealTimeClient> _sclients;
//         private readonly ConcurrentQueue<UnhandledRawPacket> _unhandledMessages;
//         private readonly CancellationTokenSource _cancellation;
//         private readonly AutoResetEvent _newMessage;
//
//         public SocketServer(CancellationToken cancellation)
//         {
//             _sclients = new ConcurrentDictionary<string, RealTimeClient>();
//             _newMessage = new AutoResetEvent(false);
//             _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
//         }
//
//         // The start method should be called when the server is bound to a port.
//         // Messages will be received, but will not be processed unless/until the Start method is called
//         public Task Start()
//         {
//             // Start a dedicated thread to process messages so that the ChannelRead operation does not block
//             return Task.Run(() =>
//             {
//                 while (!_cancellation.IsCancellationRequested)
//                 {
//                     _newMessage.WaitOne(50); // Sleep until a new message arrives
//
//                     while (_unhandledMessages.TryDequeue(out var packet))
//                         // Process each message in the queue, then sleep until new messages arrive
//                     {
//                         Task.Run(() =>
//                         {
//                             ProcessPacket(packet);
//                         });
//                     }
//                 }
//             }, _cancellation.Token);
//         }
//
//         private void ProcessPacket(UnhandledRawPacket packet)
//         {
//             // switch (Packet.Command)
//             // {
//             //     case Command.Register:
//             //         // Register a new client, or update an existing client with a new Context
//             //         var sclient = new RealTimeClient(Packet.ClientId, Packet.Context);
//             //         _sclients.AddOrUpdate(Packet.ClientId, sclient, (_, __) => sclient);
//             //         break;
//             //     case Command.SendToClient:
//             //         RealTimeClient destinationClient;
//             //         using (var reader = new JsonTextReader(new StringReader(Packet.Data)))
//             //         {
//             //             var sendToClientCommand = serializer.Deserialize<SendToClientCommand>(reader);
//             //             if (_sclients.TryGetValue(sendToClientCommand.DestinationClientId,
//             //                 out destinationClient))
//             //             {
//             //                 var clientMessage = new Packet
//             //                 {
//             //                     ClientId = Packet.ClientId,
//             //                     Command = sendToClientCommand.ClientCommand,
//             //                     Data = sendToClientCommand.Data
//             //                 };
//             //                 destinationClient.Context.Channel.WriteAndFlushAsync(clientMessage);
//             //             }
//             //         }
//             //
//             //         break;
//             // }
//         }
//
//         // Receive each message from the clients and enqueue them to be procesed by the dedicated thread
//         protected override void ChannelRead0(IChannelHandlerContext context, byte[] data)
//         {
//             _unhandledMessages.Enqueue(new UnhandledRawPacket(new RawPacket(data), context));
//             _newMessage.Set(); // Trigger an event so that the thread processing messages wakes up when a new message arrives
//         }
//         
//         
//
//         // Flush the channel once the Read operation has completed
//         public override void ChannelReadComplete(IChannelHandlerContext context)
//         {
//             context.Flush();
//             base.ChannelReadComplete(context);
//         }
//
//         // Automatically stop the message-processing thread when this object is disposed
//         public void Dispose()
//         {
//             _cancellation.Cancel();
//         }
//     }
// }