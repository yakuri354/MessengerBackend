#nullable disable
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MessengerBackend.Models
{
    public class Room
    {
        public int RoomID { get; set; }
        [Column(TypeName = "char(11)")] public string RoomPID { get; set; }
        public IEnumerable<Message> Messages { get; set; }
        public RoomType Type { get; set; }
        public IEnumerable<RoomParticipant> Participants { get; set; }

        [NotMapped]
        public IEnumerable<User> Users =>
            Participants?.Select(p => p.User);

        public DateTime CreatedAt { get; set; }
        public string Link { get; set; }
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

        [Required] public User User { get; set; }

        public int RoomID { get; set; }

        [Required] public Room Room { get; set; }

        public ParticipantRole Role { get; set; }
    }

    public enum ParticipantRole
    {
        Creator,
        Participant
    }
}