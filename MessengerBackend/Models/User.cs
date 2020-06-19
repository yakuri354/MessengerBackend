using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MessengerBackend.Models
{
    public class User
    {
        [BsonId]
        public string ID { get; set; }
        
        public string Username { get; set; }
        public string SecondFactorPassword { get; set; } // 

        public string Number { get; set; }
        public string CountryCode { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }

        [BsonElement("Avatar")]
        public string AvatarUrl { get; set; }
    }
}