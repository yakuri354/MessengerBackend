using System.Linq;
using System.Threading.Tasks;
using MessengerBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;

namespace MessengerBackend.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly MessengerDBContext _dbContext;

        public AuthService(MessengerDBContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _configuration = config;
        }

        public Task<Session> GetSession(string token)
        {
            return _dbContext.Sessions.Where(s => s.RefreshToken == token).FirstOrDefaultAsync();
        }

        public Session GetAndDelete(string token)
        {
            var session = GetSession(token).Result;
            if (session == null) return null;
            _dbContext.Sessions.Remove(session);
            _dbContext.SaveChangesAsync();
            return session;
        }

        public EntityEntry<Session> AddSession(Session session)
        {
            var s = _dbContext.Add(session);
            _dbContext.SaveChanges();
            return s;
        }
        
    }
}