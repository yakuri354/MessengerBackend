#nullable disable
namespace MessengerBackend.Models
{
    public class Event
    {
        public long EventID { get; set; }
        public bool Delevered { get; set; }
        public EventType Type { get; set; }
        public long RelatedID { get; set; }
        public User Recepient { get; set; }
    }

    public enum EventType
    {
        Message
    }
}