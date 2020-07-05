using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Force.Crc32;
using MessengerBackend.RealTime.Protocol;
using Serilog;

namespace MessengerBackend.RealTime
{
    public class RealTimeServer : IDisposable
    {
        private static readonly ILogger _log = Log.ForContext<RealTimeServer>();
        private readonly CancellationTokenSource _cancellation;
        private readonly List<OpenConnection> _connections = new List<OpenConnection>();
        private readonly ManualResetEvent _shouldAcceptV4 = new ManualResetEvent(false);
        private readonly ManualResetEvent _shouldAcceptV6 = new ManualResetEvent(false);

        // Mostly for unit tests
        public readonly ManualResetEvent IsListeningV4 = new ManualResetEvent(false);
        public readonly ManualResetEvent IsListeningV6 = new ManualResetEvent(false);

        private Socket _sockV4;
        private Socket _sockV6;

        public RealTimeServer(CancellationTokenSource cancellationTokenSource) => _cancellation = cancellationTokenSource;

        public RealTimeServer(CancellationTokenSource cancellationTokenSource, int portV4, int portV6)
        {
            _cancellation = cancellationTokenSource;
            // This is needed for concurrent unit tests and auto port assignment with port 0
            // or maybe just custom port
            PortV4 = (ushort) portV4;
            PortV6 = (ushort) portV6;
        }

        public static ushort PortV4 { get; private set; } = 7100;
        public static ushort PortV6 { get; private set; } = 7200;

        public void Dispose()
        {
            _log.Information("Shutting socket server down");
            _cancellation.Cancel();
            // Listening thread unblocks and stops because of cancellation check in while loop condition
            _shouldAcceptV4.Set();
            _shouldAcceptV6.Set();
            _sockV4.Close();
            _sockV6.Close();
        }

        public Task Start()
        {
            return Task.Run(() =>
                {
                    _log.Debug("Opening sockets");
                    _log.Verbose("Opening IPv4 socket");
                    _sockV4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _log.Verbose("Opening IPv6 socket");
                    _sockV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        _log.Verbose("Binding IPv4 endpoint");
                        _sockV4.Bind(new IPEndPoint(IPAddress.Any, PortV4));
                        PortV4 = (ushort) ((IPEndPoint) _sockV4.LocalEndPoint).Port;
                        _log.Verbose("Binding IPv6 endpoint");
                        _sockV6.Bind(new IPEndPoint(IPAddress.IPv6Any, PortV6));
                        PortV6 = (ushort) ((IPEndPoint) _sockV6.LocalEndPoint).Port;
                    }
                    catch (SocketException ex)
                    {
                        _log.Fatal("Error in binding port:\n {}", ex.Message);
                    }

                    _log.Verbose("Listening on both sockets");
                    _sockV4.Listen(128);
                    _sockV6.Listen(128);
                    Task.WaitAll(
                        Task.Run(() =>
                        {
                            while (!_cancellation.IsCancellationRequested)
                            {
                                _shouldAcceptV4.Reset();
                                _sockV4.BeginAccept(HandleConnection, _sockV4);
                                IsListeningV4.Set();
                                _shouldAcceptV4.WaitOne();
                            }
                        }),
                        Task.Run(() =>
                        {
                            while (!_cancellation.IsCancellationRequested)
                            {
                                _shouldAcceptV6.Reset();
                                _sockV6.BeginAccept(HandleConnection, _sockV6);
                                IsListeningV6.Set();
                                _shouldAcceptV6.WaitOne();
                            }
                        }));
                },
                _cancellation.Token);
        }


        private void HandleConnection(IAsyncResult result)
        {
            var globalSocket = (Socket) result.AsyncState;

            // Receive packet header and decide buffer length
            var headerBuffer = new byte[6];

            var socket = globalSocket!.EndAccept(result);
            var connection = new OpenConnection(socket);
            connection.Logger = Log.ForContext("ConnectionID", connection.UserPublicID);
            _connections.Add(connection);
            connection.Logger.Information(
                "Connection Established; EP: {@RemoteEndPoint}", socket.RemoteEndPoint);

            connection.Logger.Debug("Receiving packet header");
            socket.Receive(headerBuffer, 0, 6, SocketFlags.None);

            Packet packet;

            try
            {
                packet.Type = (PacketType) BitConverter.ToInt16(headerBuffer[..2]);
                packet.Size = BitConverter.ToUInt32(headerBuffer[2..]);
            }
            catch (Exception e) when (e is SocketException || e is ArgumentOutOfRangeException)
            {
                connection.Logger.Warning("Failed to deserialize packet header");
                // TODO Proper error handling
                socket.Close();
                return;
            }

            connection.Logger.Debug("Packet length calculated, receiving payload and checksum");
            var buffer = new byte[packet.Size];
            Array.Copy(headerBuffer, buffer, headerBuffer.Length);
            socket.Receive(buffer, 6, (int) (packet.Size - 6), SocketFlags.None);
            packet.Payload = buffer[6..^4];
            if (!Crc32CAlgorithm.IsValidWithCrcAtEnd(buffer))
                // throw new NotImplementedException(); // TODO Implement proper error handling
                return;
            if (packet.Payload.Length == 4 && Encoding.ASCII.GetString(packet.Payload).ToLower() == "ping")
            {
                socket.Send(Encoding.ASCII.GetBytes("PONG"));
                socket.Close();
            }

            // TODO
            socket.Close();
        }
    }
}