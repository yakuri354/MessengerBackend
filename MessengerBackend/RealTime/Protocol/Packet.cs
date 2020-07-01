using System;
using Force.Crc32;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace MessengerBackend.RealTime.Protocol
{
    public struct Packet
    {
        public PacketType Type;

        private const int _metadataSize = 10;

        //   _____________________________
        //  |            Header           |
        //  |           6 bytes           |
        //  |----------|------------------|---------------------------------|--------------------|
        //  |   Type   |       Size       |            Payload              |  CRC32C Checksum   |
        //  |  2 bytes |      4 bytes     |        Anywhere to 25 Kb        |      4 bytes       |
        //  |__________|__________________|_________________________________|____________________|
        public uint Size; // Payload and Checksum size in bytes

        public byte[] Payload; // Most likely ProtoBuf encoded data
        // CRC32C checksum is computed in the end

        public Packet(byte[] payload, PacketType type)
        {
            Type = type;
            Payload = payload;
            Size = (uint) (payload.Length + _metadataSize);
        }

        public byte[] GetBytes()
        {
            var data = new byte[Size];
            Array.Copy(BitConverter.GetBytes((ushort) Type), 0, data, 0, 2);
            Array.Copy(BitConverter.GetBytes(Size), 0, data, 2, 4);
            Array.Copy(Payload, 0, data, 6, Payload.Length);
            Crc32CAlgorithm.ComputeAndWriteToEnd(data);
            return data;
        }
    }


    public enum PacketType : ushort
    {
        // Client
        Authorization = 0x01,
        Register = 0x02,
        OutboundMessage = 0x10,

        // Server
        InboundMessage = 0x50,

        Ping = 0xFF
    }

    public enum RealTimeConnectionError : ushort
    {
        BadPacket = 0x01,
        ChecksumCorrupted = 0x02,
        UnknownPacketType = 0x03,
        PacketTooBig = 0x04
    }
}