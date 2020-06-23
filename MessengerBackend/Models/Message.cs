using System.ComponentModel.DataAnnotations.Schema;

namespace MessengerBackend.Models
{
    public class Message
    {
        [Column(TypeName = "uint8")]
        public int ID;
    }
}