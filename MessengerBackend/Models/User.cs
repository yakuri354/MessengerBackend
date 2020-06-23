using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Actor
    {
        public int ID { get; set; }
        [Column(TypeName = "varchar(32)")]
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }
    
    public class User : Actor
    {
        [Column(TypeName = "varchar(15)")]
        public string Number { get; set; }
        [Column(TypeName = "varchar(3)")]
        public string CountryCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string Bio { get; set; }
        
        public string TwilioUID { get; set; }

        public IEnumerable<Session> Sessions;
        public IEnumerable<Room> Rooms;
    }
    
    public class Bot : Actor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsVerified { get; set; }

        public string Token { get; set; }
    }
}