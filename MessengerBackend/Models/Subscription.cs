using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace MessengerBackend.Models
{
    public class Subscription
    {
        public long SubscriptionID { get; set; }

        public User User { get; set; }

        public Room Room { get; set; }
        public SubscriptionType Type { get; set; }
        public DateTime SubscribedAt { get; set; }
        public DateTime LastEventOccuredAt { get; set; }
        public IEnumerable<Event> Events { get; set; }

        [Column(TypeName = "char(11)")] public string SubscriptionPID { get; set; }
    }

    public enum SubscriptionType
    {
        Message
    }
}