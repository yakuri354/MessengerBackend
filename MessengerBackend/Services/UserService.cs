using MessengerBackend.Models;
using MongoDB.Driver;

namespace MessengerBackend.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService()
        {
            _users = Program.DB.GetCollection<User>("users");
            
        }
    }
}