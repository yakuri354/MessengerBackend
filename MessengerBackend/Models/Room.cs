using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Room
    {
        public int RoomID { get; set; }
        [Column(TypeName = "char(11)")] public string RoomPID { get; set; }
        public IEnumerable<Message> Messages { get; set; }
        public RoomType Type { get; set; }
        public IEnumerable<RoomParticipant> Participants { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
        public string RoomAvatar { get; set; }
    }

    public enum RoomType
    {
        Private,
        Group,
        Channel
    }

    public class RoomParticipant
    {
        public int UserID { get; set; }
        public User User { get; set; }
        public int RoomID { get; set; }
        public Room Room { get; set; }
    }
}