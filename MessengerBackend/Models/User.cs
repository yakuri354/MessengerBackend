using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Quickford;

namespace MessengerBackend.Models
{
    public class Actor
    {
        [Required]
        [Column(TypeName = "int")]
        public int ID { get; set; }
        [Column(TypeName = "varchar(32)")]
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }
    
    public class User : Actor
    {
        [Required]
        [Column(TypeName = "varchar(18)")]
        public string Number { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; }
        [Column(TypeName = "varchar(256)")]
        public string Bio { get; set; }

        [Required]
        [Column(TypeName = "int")]
        private uint RandomUID { get; set; }

        [NotMapped]
        public string PublicUID => Base32.Encode(RandomUID);

        public IEnumerable<Session> Sessions;
        public IEnumerable<Room> Rooms;
        
    }
    
    public class Bot : Actor
    {
        public string Name { get; set; }
        public string Description { get; set; }

        [Column(TypeName = "char(32)")]
        public string Token { get; set; }
    }
}