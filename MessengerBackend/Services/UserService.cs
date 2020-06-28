using System;
using MessengerBackend.Models;

namespace MessengerBackend.Services
{
    public class UserService
    {
        private MessengerDBContext _dbContext;

        protected UserService(MessengerDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public User Add(string number, string firstName, string lastName)
        {
            var newUser = _dbContext.Users.Add(
                new User { Number = number, 
                                 FirstName = firstName,
                                 LastName = lastName });

            _dbContext.SaveChanges();
            return newUser.Entity;
        }
    }
}