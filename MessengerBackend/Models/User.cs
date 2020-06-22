using System;
using System.Collections;
using System.Collections.Generic;
namespace MessengerBackend.Models
{
    public class ChatEntity
    {
        public Guid ID { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }
    
    public class User : ChatEntity
    {
        public string Number { get; set; }
        public string CountryCode { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }

        public IEnumerable<Session> Sessions;
    }
    
    public class Bot : ChatEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsVerified { get; set; }

        public string Token { get; set; }
    }
}