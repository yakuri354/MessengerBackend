using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MessengerBackend.Models
{
    public class Session
    {
        [Required]
        public Guid ID;
        [Required]
        public User User;
        public string Fingerprint;
        public string UserAgent;
        public IPAddress IP;
        
        public SessionType Type;
        public SessionPlatform Platform;

        [Required]
        public ulong ExpiresIn;
        [Required]
        public DateTime CreatedAt;
        [Required]
        public DateTime UpdatedAt;
    }

    public enum SessionPlatform
    {
        macOS,
        Windows,
        Linux,
        ChromeOS,
        Android,
        IOS
    }

    public enum SessionType
    {
        Mobile,
        Desktop,
        Web
    }
}