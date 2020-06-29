using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using JWT.Builder;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Encoders;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserService _userService;
        private readonly VerificationService _verificationService;
        private readonly AuthService _authService;
        private readonly CryptoService _cryptoService;


        private readonly List<NumberBan> _numberBans = new List<NumberBan>();

        public AuthController(
            UserService userService,
            VerificationService verificationService,
            AuthService authService,
            CryptoService cryptoService)
        {
            _userService = userService;
            _verificationService = verificationService;
            _authService = authService;
            _cryptoService = cryptoService;
        }

        [HttpPost("sendCode")]
        public async Task<IActionResult> SendCode()
        {
            var input =
                JsonConvert.DeserializeAnonymousType(Request.Body.GetString(), new
                {
                    number = "",
                    channel = ""
                });

            if (input == null || !Regex.Match(input.number, @"^\+[1-9]\d{1,14}$").Success)
                return BadRequest();
            var currentBan = _numberBans.FirstOrDefault(q => q.Number == input.number);
            if (currentBan != null)
            {
                Response.Headers.Add("Retry-After", (currentBan.ExpiresAt - DateTime.Now).Seconds.ToString());
                return new StatusCodeResult((int) HttpStatusCode.TooManyRequests);
            }

            var result = await _verificationService.StartVerificationAsync(input.number, input.channel);
            if (!result.IsValid) return BadRequest(result.Error);

            var ban = new NumberBan // TODO FIXME
            {
                Number = input.number,
                ExpirationTimer = new Timer(_verificationService.ResendInterval),
                ExpiresAt = DateTime.Now.AddSeconds(_verificationService.ResendInterval)
            };

            _numberBans.Add(ban);
            ban.ExpirationTimer.Elapsed += (source, e) => { _numberBans.Remove(ban); };
            ban.ExpirationTimer.Enabled = true;

            return Ok();
        }

        [HttpPost("verifyCode")]
        public async Task<IActionResult> VerifyCode()
        {
            var input = JsonConvert.DeserializeAnonymousType(
                Request.Body.ToString(), new
                {
                    number = "",
                    code = ""
                });

            if (input == null) return BadRequest();

            if (input.code.Length != _verificationService.TwilioService.CodeLength) return Forbid();


            var result = await _verificationService.CheckVerificationAsync(input.number, input.code);
            if (!result.IsValid) return Forbid(result.Error);


            return Ok(JsonConvert.SerializeObject(new
            {
                registrationToken =
                    _cryptoService.JwtBuilder
                        .ExpirationTime(DateTime.Now.AddMinutes(20))
                        .AddClaim("type", "reg")
                        .AddClaim("num", input.number)
                        .Encode()
            }));
        }

        [HttpPost("register")]
        public IActionResult Register()
        {
            var input = JsonConvert.DeserializeAnonymousType(
                Request.Body.ToString(), new
                {
                    registerToken = "",
                    firstName = "",
                    lastName = "",
                    fingerprint = ""
                });

            if (input == null) return BadRequest();
            var token = _cryptoService.JwtBuilder
                .MustVerifySignature()
                .Decode<IDictionary<string, string>>(input.registerToken);
            if (token["type"] != "reg" || !token.ContainsKey("number"))
                return BadRequest("Token must have \"type\" == \"reg\"");

            var session =
                _authService.AddSession(new Session
                {
                    CreatedAt = DateTime.UtcNow,
                    ExpiresIn = AuthService.RefreshTokenLifetimeDays,
                    Fingerprint = input.fingerprint,
                    IPHash = SHA256.Create()
                        .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes()),
                    UserAgent = Request.Headers[HttpRequestHeader.UserAgent.ToString()],
                    User = _userService.Add(token["number"], input.firstName, input.lastName),
                    UpdatedAt = DateTime.UtcNow,
                    RefreshToken = AuthService.GenerateToken(AuthService.RefreshTokenLength)
                });

            return Ok(JsonConvert.SerializeObject(new
            {
                refreshToken = session.Entity.RefreshToken,
                accessToken = _cryptoService.JwtBuilder
                    .AddClaim("type", "access")
                    .AddClaim("jti", AuthService.GenerateToken(AuthService.AccessTokenJtiLength))
                    .AddClaim("ip",
                        Base64.ToBase64String(SHA256.Create()
                            .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes())))
                    .AddClaim("user", session.Entity.User.PublicUID)
                    .ExpirationTime(DateTime.UtcNow.AddDays(AuthService.RefreshTokenLifetimeDays))
                    .Encode()
            }));
        }

        [HttpPost("refresh")]
        public IActionResult Refresh()
        {
            var input = JsonConvert.DeserializeAnonymousType(Request.Body.GetString(), new
            {
                fingerprint = ""
            });
            var token = Request.Headers[HeaderNames.Authorization];

            var session = _authService.GetAndDelete(token);
            if (session == null) return BadRequest();
            if (session.Fingerprint != input.fingerprint
                || session.IPHash != HttpContext.Connection.RemoteIpAddress.GetAddressBytes()
                || session.UserAgent != Request.Headers["User-Agent"])
                return Forbid(JsonConvert.SerializeObject(
                    new
                    {
                        error = "Token Verification Failed"
                    }));
            if (session.CreatedAt.AddSeconds(session.ExpiresIn) >= DateTime.UtcNow)
                return Forbid(JsonConvert.SerializeObject(
                    new
                    {
                        error = "Token Expired"
                    }));
            var newSession = _authService.AddSession(new Session
            {
                CreatedAt = DateTime.Now,
                ExpiresIn = (DateTime.Now.AddDays(AuthService.RefreshTokenLifetimeDays) - DateTime.Now).Seconds,
                Fingerprint = input.fingerprint,
                IPHash = session.IPHash,
                UserAgent = session.UserAgent,
                UpdatedAt = DateTime.Now,
                User = session.User,
                RefreshToken = AuthService.GenerateToken(AuthService.RefreshTokenLength)
            });
            return Ok(JsonConvert.SerializeObject(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                accessToken = _cryptoService.JwtBuilder
                    .AddClaim("type", "access")
                    .AddClaim("jti", AuthService.GenerateToken(AuthService.AccessTokenJtiLength))
                    .AddClaim("ip",
                        Base64.ToBase64String(SHA256.Create()
                            .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes())))
                    .AddClaim("user", newSession.Entity.User.PublicUID)
                    .ExpirationTime(DateTime.UtcNow.AddDays(AuthService.RefreshTokenLifetimeDays))
                    .Encode()
            }));
        }

        private class NumberBan
        {
            public string Number;
            public Timer ExpirationTimer;
            public DateTime ExpiresAt;
        }
    }
}