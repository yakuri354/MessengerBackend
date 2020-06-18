using System.Text;
using DotNetty.Transport.Channels;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace MessengerBackend.RealTime.Protocol
{
    public struct Packet
    {
        public PacketType Type;
        //  /____________Header___________\  
        //  |----------|------------------|---------------------------------|--------------------|
        //  |   Type   |       Size       |        Protobuf Payload         |   CRC32 Checksum   |
        //  |  2 bytes |      4 bytes     |        Anywhere to 25 Kb        |      4 bytes       |
        //  |----------|------------------|---------------------------------|--------------------|
        public uint Size; // Payload and Checksum size in bytes
        public byte[] Payload; // ProtoBuf encoded data
        public uint Checksum; // CRC32 Checksum
    }
    
    
    public enum PacketType : short
    {
        // Client
        Authorization = 0x01,
        Register = 0x02,
        OutboundMessage = 0x10,
        // Server
        InboundMessage = 0x50
    }
    
    public readonly struct RawPacket
    {
        public RawPacket(byte[] data)
        {
            Data = data;
        }

        public readonly byte[] Data;
    }

    public struct UnhandledRawPacket
    {
        public RawPacket RawPacket;
        public IChannelHandlerContext Context;

        public UnhandledRawPacket(RawPacket rawPacket, IChannelHandlerContext context)
        {
            RawPacket = rawPacket;
            Context = context;
        }
    }
}