using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Room
    {
        [Column(TypeName = "uint8")]
        public long ID;
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