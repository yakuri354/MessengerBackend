using System;
using System.Collections.Generic;

namespace MessengerBackend.Models
{
    public class Room
    {
        public int RoomID { get; set; }
        public IEnumerable<Message> Messages { get; set; }
        public RoomType Type { get; set; }
        public IEnumerable<User> Users { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public enum RoomType
    {
        Private,
        Group,
        Channel
    }
}