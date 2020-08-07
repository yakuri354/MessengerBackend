using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace MessengerBackend.Models
{
#nullable disable
    public class Session
    {
        [Required] public DateTime CreatedAt { get; set; }

        [Required] public int ExpiresIn { get; set; }

        public string Fingerprint { get; set; }

        [Column(TypeName = "bigint")] public long SessionID { get; set; }

        public byte[] IPHash { get; set; }

        [Required] public string RefreshToken { get; set; }

        [Required] public DateTime UpdatedAt { get; set; }

        [Required] public User User { get; set; }

        public string UserAgent { get; set; }
    }
#nullable enable
}