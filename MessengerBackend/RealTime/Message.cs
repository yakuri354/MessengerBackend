using System.Collections.Generic;
using MessagePack;

namespace MessengerBackend.RealTime
{
    public interface IMessage
    {
        public uint ID { get; set; }
    }

    [MessagePackObject]
    public struct InboundMessage : IMessage
    {
        [Key(0)] public InboundMessageType Type;
        [Key(1)] public uint ID { get; set; }
        [Key(2)] public string? Method { get; set; }
        [Key(3)] public List<object>? Params { get; set; }
    }

    public enum InboundMessageType
    {
        Method,
        Subscribe,
        Unsubscribe,
        Connect,
        Auth
    }

    [MessagePackObject]
    public struct OutboundMessage : IMessage
    {
        [Key(0)] public OutboundMessageType Type;
        [Key(1)] public uint ID { get; set; }
        [Key(2)] public bool IsSuccess { get; set; }
        [Key(3)] public List<object>? Data { get; set; }
    }

    public enum OutboundMessageType
    {
        Response,
        Event
    }
}