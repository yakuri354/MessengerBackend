#nullable disable
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Message
    {
        public int MessageID { get; set; }

        [Column(TypeName = "char(11)")]
        [Required]
        public string MessagePID { get; set; }

        [Required] public virtual Room TargetRoom { get; set; }

        public int TargetRoomID { get; set; }

        public string Text { get; set; }
        public User Sender { get; set; }
        public DateTime SentAt { get; set; }
        public Message ReplyTo { get; set; }
    }
}