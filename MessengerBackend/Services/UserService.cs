using System;
using System.Linq;
using MessengerBackend.Database;
using MessengerBackend.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MessengerBackend.Services
{
    public class UserService
    {
        private readonly MessengerDBContext _dbContext;

        public UserService(MessengerDBContext dbContext) => _dbContext = dbContext;

        public User Add(string number, string firstName, string lastName)
        {
            var newUser = new User
            {
                Number = number,
                FirstName = firstName,
                LastName = lastName
            };
            try
            {
                var user = _dbContext.Users.Add(newUser);
                _dbContext.SaveChanges();
                return user.Entity;
            }
            catch (DbUpdateException e)
                when ((e.InnerException as PostgresException)?.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return null;
            }
        }

        public User FindOne(string username = null, string uid = null, int id = 0, string number = null)
        {
            return _dbContext.Users.FirstOrDefault(u =>
                u.Username == username && username != null
                || u.Number == number && number != null
                || u.UserPID == uid && uid != null
                || u.UserID == id && id != 0);
        }


        public User FindOneStrict(string username = null, string uid = null, int id = 0, string number = null)
        {
            if (username == null && uid == null && id == 0 && number == null) throw new ArgumentNullException();
            return _dbContext.Users.FirstOrDefault(u =>
                (u.Username == username || username == null)
                && (u.Number == number || number == null)
                && (u.UserPID == uid || uid == null)
                && (u.UserID == id || id == 0));
        }

        public void SaveUser(User user)
        {
            _dbContext.Users.Attach(user);
            _dbContext.SaveChanges();
        }
    }
}