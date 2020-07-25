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

        public User FirstOrDefault(Func<User, bool> input) => _dbContext.Users.FirstOrDefault(input);
        public bool Any(Func<User, bool> input) => _dbContext.Users.Any(input);

        public bool SaveUser(User user)
        {
            try
            {
                _dbContext.Users.Attach(user);
                _dbContext.SaveChanges();
                return true;
            }
            catch (DbUpdateException e)
                when ((e.InnerException as PostgresException)?.SqlState.EqualsAnyString(
                    PostgresErrorCodes.UniqueViolation,
                    PostgresErrorCodes.StringDataRightTruncation) ?? false)
            {
                return false;
            }
        }
    }
}