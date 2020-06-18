using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;
using MessengerBackend.RealTime.Protocol;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace MessengerBackend.RealTime
{
    public class RealTimeServer : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;

        public ushort PortV4 { get; } = 7100;
        public ushort PortV6 { get; } = 7200;
        private List<OpenConnection> _connections = new List<OpenConnection>();
        private ManualResetEvent _shouldAcceptV4 = new ManualResetEvent(false);
        private ManualResetEvent _shouldAcceptV6 = new ManualResetEvent(false);

        private Socket _sockv4;
        private Socket _sockv6;

        private static Serilog.ILogger _log = Log.ForContext < RealTimeServer > ();

        public RealTimeServer(CancellationTokenSource cancellationTokenSource)
        {
            _cancellation = cancellationTokenSource;
        }

        public Task Start()
        {
            _log.Information("Starting realtime server...");
            return Task.Run(() =>
                {
                    _log.Debug("Opening sockets");
                    _log.Verbose("Opening IPv4 socket");
                    _sockv4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _log.Verbose("Opening IPv6 socket");
                    _sockv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    _log.Verbose("Binding IPv4 socket");
                    _sockv4.Bind(new IPEndPoint(IPAddress.Any, PortV4));
                    _log.Verbose("Binding IPv6 socket");
                    _sockv6.Bind(new IPEndPoint(IPAddress.IPv6Any, PortV6));
                    _log.Verbose("Listening on both sockets");
                    _sockv4.Listen(128);
                    _sockv6.Listen(128);
                    var IPv4Task = Task.Run(() =>
                    {
                        while (!_cancellation.IsCancellationRequested)
                        {
                            _shouldAcceptV4.Reset();
                            _sockv4.BeginAccept(HandleConnection, _sockv4);
                            _shouldAcceptV4.WaitOne();
                        }
                    });
                    var IPv6Task = Task.Run(() =>
                    {
                        while (!_cancellation.IsCancellationRequested)
                        {
                            _shouldAcceptV6.Reset();
                            _sockv6.BeginAccept(HandleConnection, _sockv6);
                            _shouldAcceptV6.WaitOne();
                        }
                    });
                    Task.WaitAll(IPv4Task, IPv6Task);
                },
                _cancellation.Token);
        }
        

        private void HandleConnection(IAsyncResult result)
        {
            
            var globalSocket = (Socket) result.AsyncState;
            
            var connection = new OpenConnection(globalSocket);
            connection._logger = Log.ForContext("ConnectionID", connection.ID);
            _connections.Add(connection);

            // Receive packet header and decide buffer length
            var headerBuffer = new byte[6];

            var socket = globalSocket.EndAccept(result);
            connection._logger.Information("Connection Established; EP: {@RemoteEndPoint}", socket.RemoteEndPoint);

            connection._logger.Debug("Receiving packet header");
            socket.Receive(headerBuffer, 0, 6, SocketFlags.None);

            Packet packet;
            
            PacketType type; // TODO Packet type handling
            uint size;
            try
            {
                packet.Type = (PacketType) BitConverter.ToInt16(headerBuffer[..1]);
                packet.Size = BitConverter.ToUInt32(headerBuffer[2..5]);
            }
            catch (InvalidCastException)
            {
                connection._logger.Warning("Failed to deserialize packet header");
                // TODO Proper error handling
                socket.Close();
                return;
            }
            connection._logger.Debug("Packet length calculated, receiving payload and checksum");
            var buffer = new byte[packet.Size];
            Array.Copy(headerBuffer, buffer, headerBuffer.Length);
            socket.Receive(buffer, 6, Convert.ToInt32(packet.Size - 6), SocketFlags.None);
            packet.Payload = buffer[6..^4];
            packet.Checksum = BitConverter.ToUInt32(buffer[^3..]);
            if (!Crc32Algorithm.IsValidWithCrcAtEnd(buffer)) return; // TODO Implement proper error handling
            if (packet.Payload.Length == 4 && Encoding.ASCII.GetString(packet.Payload).ToLower() == "ping")
            {
                socket.Send(Encoding.ASCII.GetBytes("PONG"));
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            // Listening thread unblocks and stops because of cancellation check in while loop condition
            _shouldAcceptV4.Set();
            _shouldAcceptV6.Set();
            _sockv4.Close();
            _sockv6.Close();
        }
    }
}