#define USEHMAC

using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;

namespace MessengerBackend.Services
{
    public class CryptoService
    {
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly IConfiguration _configuration;
#if USERSA
        public JwtBuilder JwtBuilder => new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(PublicKey, PrivateKey))
            .Issuer(JwtOptions.Issuer)
            .Audience(JwtOptions.Audience);

        public readonly RSACryptoServiceProvider PrivateKey;

        public readonly RSACryptoServiceProvider PublicKey;
#elif USEHMAC
        public JwtBuilder JwtBuilder => new JwtBuilder()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(HMACKey)
            .Issuer(JwtOptions.Issuer)
            .Audience(JwtOptions.Audience);

        public readonly string HMACKey;
#endif
        public readonly SHA256 Sha256;

        public bool IPValid(IPAddress realIP, string base64EncodedIP) =>
            Sha256
                .ComputeHash(realIP.GetAddressBytes())
                .SequenceEqual(Convert.FromBase64String(base64EncodedIP));

        public CryptoService(IConfiguration configuration)
        {
            _configuration = configuration;
            Sha256 = SHA256.Create();

#if USERSA
            using (var sr = new StringReader(_configuration["JWT:RSAPublicKey"]))
            {
                PublicKey = new RSACryptoServiceProvider();
                PublicKey.ImportParameters(new PemReader(sr).ReadRsaKey());
            }

            using (var sr = new StringReader(_configuration["JWT:RSAPrivateKey"]))
            {
                PrivateKey = new RSACryptoServiceProvider();
                PrivateKey.ImportParameters(new PemReader(sr).ReadRsaKey());
            }
#elif USEHMAC
            HMACKey = _configuration["JWT:HMACKey"];
#endif
        }

        public static string GenerateRefreshToken() => GenerateToken(JwtOptions.RefreshTokenLength);

        private static string GenerateCrockford(int len) => GenerateToken(len, CrockfordCharSet);

        private static string GenerateJti() => GenerateToken(JwtOptions.AccessTokenJtiLength);

        public static string GeneratePID(string prefix) => string.Concat(prefix, GenerateCrockford(PIDLength));

        public string CreateAccessJwt(IPAddress ip, string uid) =>
            JwtBuilder
                .AddClaim("type", "access")
                .AddClaim("jti", GenerateJti())
                .AddClaim("ip",
                    Convert.ToBase64String(Sha256
                        .ComputeHash(ip.GetAddressBytes())))
                .AddClaim("uid", uid)
                .ExpirationTime(DateTime.UtcNow.AddMinutes(JwtOptions.AccessTokenLifetimeMinutes))
                .Encode();

        public string CreateAuthJwt(IPAddress ip, string number) =>
            JwtBuilder
                .ExpirationTime(DateTime.Now.AddMinutes(20))
                .AddClaim("type", "auth")
                .AddClaim("num", number)
                .AddClaim("ip", Convert.ToBase64String(Sha256
                    .ComputeHash(ip.GetAddressBytes())))
                .ExpirationTime(DateTime.UtcNow.AddMinutes(JwtOptions.AuthTokenLifetimeMinutes))
                .Encode();

        private static string GenerateToken(int length, string charset = CharSet)
        {
            var chars = charset.ToCharArray();
            var data = new byte[length];
            _rng.GetNonZeroBytes(data);
            var result = new StringBuilder(length);
            foreach (var b in data) result.Append(chars[b % chars.Length]);
            return result.ToString();
        }

        private const string CharSet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const string CrockfordCharSet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        public static int CharSetLength = CharSet.Length;

        public static class JwtOptions
        {
            public const string Issuer = "backend";
            public const string Audience = "user";
            public const int RefreshTokenLifetimeDays = 30;
            public const int AccessTokenLifetimeMinutes = 20;
            public const int AuthTokenLifetimeMinutes = AccessTokenLifetimeMinutes;
            public const int AccessTokenJtiLength = 10;
            public const int RefreshTokenLength = 24;
        }

        private const int PIDLength = 10;
    }
}