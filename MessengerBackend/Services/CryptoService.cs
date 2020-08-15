#define USEHMAC

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MessengerBackend.Services
{
    public class CryptoService
    {
        private static readonly RNGCryptoServiceProvider Rng = new RNGCryptoServiceProvider();

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
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
        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

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
            TokenValidationParameters = new TokenValidationParameters
            {
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuer = JwtOptions.Issuer,
                ValidateAudience = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidAudience = JwtOptions.Audience,
#if USERSA
                    IssuerSigningKey = new RsaSecurityKey(cryptoService.PublicKey)
#elif USEHMAC
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(HMACKey))
#endif
            };
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
            Rng.GetNonZeroBytes(data);
            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }

        public static uint RandomUint()
        {
            var buf = new byte[4];
            Rng.GetNonZeroBytes(buf);
            return BitConverter.ToUInt32(buf);
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

        public TokenValidationParameters TokenValidationParameters;

        public (SecurityToken, ClaimsPrincipal) ValidateAccessJWT(string jwt)
        {
            var principal = _jwtSecurityTokenHandler.ValidateToken(jwt, TokenValidationParameters, out var token);
            return (token, principal);
        }

        private const int PIDLength = 10;
    }
}