using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace MessengerBackend.Models
{
    public class Session
    {
        [Required]
        [Column(TypeName = "bigint")]
        public long ID;
        [Required]
        public User User;
        public string Fingerprint;
        public string UserAgent;
        public IPAddress IP;

        [Required] 
        public string RefreshToken;

        [Required]
        public int ExpiresIn;
        [Required]
        public DateTime CreatedAt;
        [Required]
        public DateTime UpdatedAt;
    }

}