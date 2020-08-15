using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace MessengerBackend.RealTime
{
    public interface IMessage
    {
        public uint ID { get; set; }
    }

    [DataContract]
    public class InboundMessage : IMessage
    {
        [DataMember(Order = 0)] public InboundMessageType Type;
        [DataMember(Order = 2)] public string? Method { get; set; }

        // [JsonConverter(typeof(JsonInt32Converter))]
        [DataMember(Order = 3)] public List<object>? Params { get; set; }

        [DataMember(Order = 1)] public uint ID { get; set; }
    }

    public enum InboundMessageType
    {
        Connect,
        Auth,
        Method,
        Subscribe,
        Unsubscribe,
        Response
    }

    [DataContract]
    public class OutboundMessage : IMessage
    {
        [DataMember(Order = 0)] public OutboundMessageType Type;
        [DataMember(Order = 2)] public bool IsSuccess { get; set; }

        // [JsonConverter(typeof(JsonInt32Converter))]
        [DataMember(Order = 3)] public Dictionary<string, object?>? Data { get; set; }

        [DataMember(Order = 1)] public uint ID { get; set; }
    }

    public enum OutboundMessageType
    {
        Response,
        Event
    }
}