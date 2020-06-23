
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
        
        // public void AddNewUser()
    }
}