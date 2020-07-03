using System;

namespace MessengerBackend.Models
{
    public class Message
    {
        public int MessageID { get; set; }
        public Room TargetRoom { get; set; }
        public string Text { get; set; }
        public User Sender { get; set; }
        public DateTime SentAt { get; set; }
        public Message ReplyTo { get; set; }
    }
}