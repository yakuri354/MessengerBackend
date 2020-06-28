using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Room
    {
        public int ID;
        public RoomType Type;
        public IEnumerable<User> Users;
        public IEnumerable<Message> Messages;
        public DateTime DateCreated { get; set; }
    }

    public enum RoomType
    {
        Private,
        Group,
        Channel
    }
}