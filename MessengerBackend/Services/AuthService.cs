using System;
using System.IO;
using MessengerBackend.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Base62;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32.SafeHandles;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MessengerBackend.Services
{
    public class AuthService
    {
        private readonly MessengerDBContext _dbContext;
        private readonly IConfiguration _configuration;

        public const int RefreshTokenLifetimeDays = 30;
        public const int TokenLength = 20;

        public readonly RSACryptoServiceProvider PublicKey;
        public readonly RSACryptoServiceProvider PrivateKey;

        public AuthService(MessengerDBContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            _configuration = config;
            using (var fs = File.OpenText(_configuration["Secrets:JWT:RSAPublicKeyPath"]))
            {
                var pemReader = new PemReader(fs);
                var keyPair = (AsymmetricCipherKeyPair) pemReader.ReadObject();
                PublicKey = new RSACryptoServiceProvider();
                PublicKey.ImportParameters(
                    DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters) keyPair.Private));
            }

            using (var fs = File.OpenText(_configuration["Secrets:JWT:RSAPrivateKeyPath"]))
            {
                var pemReader = new PemReader(fs);
                var keyPair = (AsymmetricCipherKeyPair) pemReader.ReadObject();
                PrivateKey = new RSACryptoServiceProvider();
                PrivateKey.ImportParameters(
                    DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters) keyPair.Private));
            }
        }

        public Task<Session> GetSession(string token)
        {
            return _dbContext.Sessions.Where(s => s.RefreshToken == token).FirstOrDefaultAsync();
        }

        public Task<Session> GetAndVerifySession(string token, string fingerprint, string userAgent)
        {
            return _dbContext.Sessions
                .Where(s =>
                    s.RefreshToken == token
                    && s.Fingerprint == fingerprint
                    && s.UserAgent == userAgent)
                .FirstOrDefaultAsync();
        }

        public async Task<Session> GetAndRefresh(string token, string fingerprint, string userAgent)
        {
            var session = await GetAndVerifySession(token, fingerprint, userAgent);
            if (session == null) return null;
            _dbContext.Sessions.Remove(session);
            await _dbContext.SaveChangesAsync();
            return session;
        }

        public EntityEntry<Session> AddSession(Session session)
        {
            var s = _dbContext.Add(session);
            _dbContext.SaveChanges();
            return s;
        }

        public static class AuthOptions
        {
            // TODO 
            public const string ISSUER = "backend";
            public const string AUDIENCE = "user";
            public const string JWTPublicKey = "";
        }
        
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        public static string GenerateToken(int length)
        {
            const string charSet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = charSet.ToCharArray();
            var data = new byte[length];
            _rng.GetNonZeroBytes(data);
            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }
            return result.ToString();
        }
    }
}