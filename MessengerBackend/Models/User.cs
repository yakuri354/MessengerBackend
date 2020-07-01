using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class User
    {
        public IEnumerable<Room> Rooms;


        public IEnumerable<Session> Sessions;

        [Required] [Column(TypeName = "int")] public int UserID { get; set; }

        [Column(TypeName = "varchar(32)")] public string Username { get; set; }

        public string AvatarUrl { get; set; }

        [Required]
        [Column(TypeName = "varchar(18)")]
        public string Number { get; set; }

        [Column(TypeName = "varchar(100)")] public string FirstName { get; set; }

        [Column(TypeName = "varchar(100)")] public string LastName { get; set; }

        [Column(TypeName = "varchar(256)")] public string Bio { get; set; }

        [Required]
        [Column(TypeName = "char(10)")]
        public string PublicUID { get; set; }
    }

    public class Bot
    {
        [Required] [Column(TypeName = "int")] public int BotID { get; set; }

        [Column(TypeName = "varchar(24)")] public string BotUsername { get; set; }

        public string AvatarUrl { get; set; }

        [Column(TypeName = "varchar(100)")] public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "char(24)")] public string Token { get; set; }
    }
}