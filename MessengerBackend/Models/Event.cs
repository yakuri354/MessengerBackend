using System;

#nullable disable
namespace MessengerBackend.Models
{
    public class Event
    {
        public long EventID { get; set; }
        public DateTime DeliveredAt { get; set; }
        public DateTime OccuredAt { get; set; }
        // public EventType Type { get; set; }
        public Message Message { get; set; }
        public Subscription Subscription { get; set; }
        public User Recepient { get; set; }
    }

    // public enum EventType
    // {
    //     Message
    // }
}