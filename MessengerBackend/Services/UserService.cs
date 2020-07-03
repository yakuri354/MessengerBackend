using System.Linq;
using MessengerBackend.Database;
using MessengerBackend.Models;

namespace MessengerBackend.Services
{
    public class UserService
    {
        private readonly MessengerDBContext _dbContext;

        public UserService(MessengerDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public User Add(string number, string firstName, string lastName)
        {
            var newUser = _dbContext.Users.Add(
                new User
                {
                    Number = number,
                    FirstName = firstName,
                    LastName = lastName
                });

            _dbContext.SaveChanges();
            return newUser.Entity;
        }

        public User FindOne(string username = null, string uid = null, int id = 0) =>
            _dbContext.Users.FirstOrDefault(u =>
                u.Username == username && username != null
                || u.UserPID == uid && uid != null
                || u.UserID == id && id != 0);


        public User FindOneStrict(string username = null, string uid = null, int id = 0) =>
            _dbContext.Users.FirstOrDefault(u =>
                (u.Username == username || username == null)
                && (u.UserPID == uid || uid == null)
                && (u.UserID == id || id == 0));

        public void SaveUser(User user) => _dbContext.Users.Attach(user);
    }
}