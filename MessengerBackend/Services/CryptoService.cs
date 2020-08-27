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
#if USERSA
        public JwtBuilder JwtBuilder => new JwtBuilder()
            .WithAlgorithm(new RS256Algorithm(PublicKey, PrivateKey))
            .Issuer(JwtOptions.Issuer)
            .Audience(JwtOptions.Audience);

        public readonly RSACryptoServiceProvider PrivateKey;

        public readonly RSACryptoServiceProvider PublicKey;
#elif USEHMAC
        private JwtBuilder JwtBuilder => new JwtBuilder()
            .WithAlgorithm(new HMACSHA256Algorithm())
            .WithSecret(_hmacKey)
            .Issuer(JwtOptions.Issuer)
            .Audience(JwtOptions.Audience);

        private readonly string _hmacKey;
#endif
        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        private readonly SHA256 _sha256;

        public bool IPValid(IPAddress realIP, string base64EncodedIP) =>
            _sha256
                .ComputeHash(realIP.GetAddressBytes())
                .SequenceEqual(Convert.FromBase64String(base64EncodedIP));

        public CryptoService(IConfiguration configuration)
        {
            _sha256 = SHA256.Create();

#if USERSA
            using (var sr = new StringReader(configuration["JWT:RSAPublicKey"]))
            {
                PublicKey = new RSACryptoServiceProvider();
                PublicKey.ImportParameters(new PemReader(sr).ReadRsaKey());
            }

            using (var sr = new StringReader(configuration["JWT:RSAPrivateKey"]))
            {
                PrivateKey = new RSACryptoServiceProvider();
                PrivateKey.ImportParameters(new PemReader(sr).ReadRsaKey());
            }
#elif USEHMAC
            _hmacKey = configuration["JWT:HMACKey"];
#endif
            ValidationParameters = new TokenValidationParameters
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_hmacKey))
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
                    Convert.ToBase64String(_sha256
                        .ComputeHash(ip.GetAddressBytes())))
                .AddClaim("uid", uid)
                .ExpirationTime(DateTime.UtcNow.AddMinutes(JwtOptions.AccessTokenLifetimeMinutes))
                .Encode();

        public string CreateAuthJwt(IPAddress ip, string number) =>
            JwtBuilder
                .AddClaim("type", "auth")
                .AddClaim("num", number)
                .AddClaim("ip", Convert.ToBase64String(_sha256
                    .ComputeHash(ip.GetAddressBytes())))
                .ExpirationTime(DateTime.UtcNow.AddMinutes(JwtOptions.AuthTokenLifetimeMinutes))
                .Encode();

        private static string GenerateToken(int length) =>
            GenerateToken(length, CharSet);

        private static string GenerateToken(int length, string charset)
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

        private static readonly string CharSet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly string CrockfordCharSet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        public static int CharSetLength { get; } = CharSet.Length;

        public static class JwtOptions
        {
            public static readonly string Issuer = "backend";
            public static readonly string Audience = "user";
            public static readonly int RefreshTokenLifetimeDays = 30;
            public static readonly int AccessTokenLifetimeMinutes = 20;
            public static readonly int AuthTokenLifetimeMinutes = AccessTokenLifetimeMinutes;
            public static readonly int AccessTokenJtiLength = 10;
            public static readonly int RefreshTokenLength = 24;
        }

        public TokenValidationParameters ValidationParameters { get; }

        public ClaimsPrincipal ValidateAccessJWT(string jwt) =>
            _jwtSecurityTokenHandler.ValidateToken(jwt, ValidationParameters, out _);

        private const int PIDLength = 10;
    }
}