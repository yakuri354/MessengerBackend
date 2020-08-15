using System;
using System.ComponentModel.DataAnnotations;

#nullable disable
namespace MessengerBackend.Models
{
    public class Event
    {
        public long EventID { get; set; }
        public string EventPID { get; set; }
        public DateTime DeliveredAt { get; set; }

        [Required] public DateTime OccuredAt { get; set; }

        // public EventType Type { get; set; }
        [Required] public Message Message { get; set; }

        [Required] public Subscription Subscription { get; set; }
        public long SubscriptionID { get; set; }
    }

    // public enum EventType
    // {
    //     Message
    // }
}