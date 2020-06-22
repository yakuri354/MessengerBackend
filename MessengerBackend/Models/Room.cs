using System;
using System.Collections.Generic;

namespace MessengerBackend.Models
{
    public class Room
    {
        public Guid ID;
        public RoomType Type;
        public IEnumerable<User> Users;
        public IEnumerable<Message> Messages;
    }

    public enum RoomType
    {
        Private,
        Group,
        Channel
    }
}