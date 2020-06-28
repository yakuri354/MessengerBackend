using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessengerBackend.RealTime;
using MessengerBackend.RealTime.Protocol;
using NUnit.Framework;

namespace MessengerBackendTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Ping()
        {
            TestContext.Progress.WriteLine("Starting connectivity test");
            var server = new RealTimeServer(new CancellationTokenSource(), 0, 0);
            server.Start();
            TestContext.Progress.WriteLine("Initializing sockets");
            var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socketV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            TestContext.Progress.WriteLine("Trying to connect via IPv4");
            server.IsListeningV4.WaitOne();
            socketV4.Connect(new IPEndPoint(IPAddress.Loopback, RealTimeServer.PortV4));
            var packet = new Packet(Encoding.ASCII.GetBytes("PING"), PacketType.Ping);
            socketV4.Send(packet.GetBytes());
            var pongBufV4 = new byte[4];
            TestContext.Progress.WriteLine("Receiving via IPv4");
            socketV4.Receive(pongBufV4);
            Assert.AreEqual("PONG", Encoding.ASCII.GetString(pongBufV4));
            socketV4.Close();
            TestContext.Out.WriteLine("Trying to connect via IPv6");
            server.IsListeningV6.WaitOne();
            socketV6.Connect(new IPEndPoint(IPAddress.IPv6Loopback, RealTimeServer.PortV6));
            socketV6.Send(packet.GetBytes());
            TestContext.Progress.WriteLine("Receiving via IPv6");
            var pongBufV6 = new byte[4];
            socketV6.Receive(pongBufV6);
            Assert.AreEqual("PONG", Encoding.ASCII.GetString(pongBufV6));
        }
    }
}