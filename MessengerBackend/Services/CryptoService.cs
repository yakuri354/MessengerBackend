using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using JWT.Algorithms;
using JWT.Builder;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MessengerBackend.Services
{
    public class CryptoService
    {
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly IConfiguration _configuration;

        public JwtBuilder JwtBuilder => new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(PublicKey, PrivateKey))
            .Issuer(JwtOptions.Issuer)
            .Audience(JwtOptions.Audience);
        public readonly RSACryptoServiceProvider PrivateKey;

        public readonly RSACryptoServiceProvider PublicKey;
        public readonly SHA256 Sha256;

        public CryptoService(IConfiguration configuration)
        {
            _configuration = configuration;
            Sha256 = SHA256.Create();
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
        }

        public static string GenerateRefreshToken() => GenerateToken(JwtOptions.RefreshTokenLength);
        private static string GenerateJti() => GenerateToken(JwtOptions.AccessTokenJtiLength);

        public string CreateAccessJwt(IPAddress ip, string uid)
        {
            return JwtBuilder
                .AddClaim("type", "access")
                .AddClaim("jti", GenerateJti())
                .AddClaim("ip",
                    Convert.ToBase64String(SHA256.Create()
                        .ComputeHash(ip.GetAddressBytes())))
                .AddClaim("uid", uid)
                .ExpirationTime(DateTime.UtcNow.AddDays(JwtOptions.RefreshTokenLifetimeDays))
                .Encode();
        }

        private static string GenerateToken(int length)
        {
            
            var chars = CharSet.ToCharArray();
            var data = new byte[length];
            _rng.GetNonZeroBytes(data);
            var result = new StringBuilder(length);
            foreach (var b in data) result.Append(chars[b % chars.Length]);
            return result.ToString();
        }
        
        private const string CharSet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public static int CharSetLength = CharSet.Length;

        public static class JwtOptions
        {
            public const string Issuer = "backend";
            public const string Audience = "user";
            public const int RefreshTokenLifetimeDays = 30;
            public const int AccessTokenJtiLength = 10;
            public const int RefreshTokenLength = 20;
        }
    }
}