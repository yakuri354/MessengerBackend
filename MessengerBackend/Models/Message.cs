using System;

namespace MessengerBackend.Models
{
    public class Message
    {
        public int ID { get; set; }
        public Room TargetRoom { get; set; }
        public string Text { get; set; }
        public User Sender { get; set; }
        public DateTime TimeSent { get; set; }
        public Message ReplyTo { get; set; }
    }
}