using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using JWT.Builder;
using MessengerBackend.Models;
using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly CryptoService _cryptoService;


        private readonly List<NumberBan> _numberBans = new List<NumberBan>();
        private readonly UserService _userService;
        private readonly VerificationService _verificationService;

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

        /**
         * <summary>Sends a code to specified number via specified channel</summary>
         * <param name="number">Number</param>
         * <param name="channel">Channel</param>
         * <response code="200">Code sent</response>
         * <response code="400">Code sending error</response>
         * <response code="429">Too many attempts</response>
         */
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("sendCode")]
        public async Task<IActionResult> SendCode()
        {
            var input =
                MyJsonDeserializer.DeserializeAnonymousType(Request.Body.GetString(), new
                {
                    number = "",
                    channel = ""
                });

            if (!Regex.Match(input.number, @"^\+[1-9]\d{1,14}$").Success)
                return BadRequest("bad number");
            var currentBan = _numberBans.FirstOrDefault(q => q.Number == input.number);
            if (currentBan != null)
            {
                Response.Headers.Add("Retry-After", (currentBan.ExpiresAt - DateTime.Now).Seconds.ToString());
                return new StatusCodeResult((int) HttpStatusCode.TooManyRequests);
            }

            var error = await _verificationService.StartVerificationAsync(input.number, input.channel);
            if (error != null) return BadRequest(error);

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

        [Consumes("application/json")]
        [Produces("text/plain")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("verifyCode")]
        public async Task<IActionResult> VerifyCode()
        {
            var input = MyJsonDeserializer.DeserializeAnonymousType(
                Request.Body.GetString(), new
                {
                    number = "",
                    code = ""
                });

            // if (!Regex.Match(input.number, @"^\+[1-9]\d{1,14}$").Success)
            //     return BadRequest("bad number");
            //
            // if (input.code.Length != _verificationService.TwilioService.CodeLength) return Forbid();
            //
            // var error = await _verificationService.CheckVerificationAsync(input.number, input.code);
            // if (error != null) return Forbid();


            return Ok(
                _cryptoService.JwtBuilder
                    .ExpirationTime(DateTime.Now.AddMinutes(20))
                    .AddClaim("type", "reg")
                    .AddClaim("num", input.number)
                    .AddClaim("ip", Convert.ToBase64String(_cryptoService.Sha256
                        .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes())))
                    .Encode()
            );
        }

        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("register")]
        [Authorize]
        public IActionResult Register()
        {
            var input = MyJsonDeserializer.DeserializeAnonymousType(
                Request.Body.GetString(), new
                {
                    firstName = "",
                    lastName = "",
                    fingerprint = ""
                });
            if (HttpContext.User.FindFirst("type").Value != "reg")
                return BadRequest("Invalid token");

            var newUser = _userService.Add(HttpContext.User.FindFirst("num").Value, input.firstName,
                input.lastName);

            if (newUser == null) return Forbid();

            var newSession =
                _authService.AddSession(new Session
                {
                    ExpiresIn = CryptoService.JwtOptions.RefreshTokenLifetimeDays,
                    Fingerprint = input.fingerprint,
                    IPHash = SHA256.Create()
                        .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes()),
                    UserAgent = Request.Headers[HttpRequestHeader.UserAgent.ToString()],
                    User = newUser,
                    UpdatedAt = DateTime.UtcNow,
                    RefreshToken = CryptoService.GenerateRefreshToken()
                });

            return Ok(JsonSerializer.Serialize(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                accessToken = _cryptoService.CreateAccessJwt(HttpContext.Connection.RemoteIpAddress,
                    newSession.Entity.User.UserPID)
            }));
        }

        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("refresh")]
        public IActionResult Refresh()
        {
            var input = MyJsonDeserializer.DeserializeAnonymousType(Request.Body.GetString(), new
            {
                fingerprint = ""
            });
            var token = Request.Headers[HeaderNames.Authorization];

            var session = _authService.GetAndDeleteSession(token);
            if (session == null) return BadRequest();
            if (session.Fingerprint != input.fingerprint
                || session.IPHash != HttpContext.Connection.RemoteIpAddress.GetAddressBytes()
                || session.UserAgent != Request.Headers["User-Agent"])
                return Forbid(JsonSerializer.Serialize(
                    new
                    {
                        error = "Token Verification Failed"
                    }));
            if (session.CreatedAt.AddSeconds(session.ExpiresIn) >= DateTime.UtcNow)
                return Forbid(JsonSerializer.Serialize(
                    new
                    {
                        error = "Token Expired"
                    }));
            var newSession = _authService.AddSession(new Session
            {
                ExpiresIn = (DateTime.Now.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays) - DateTime.Now)
                    .Seconds,
                Fingerprint = input.fingerprint,
                IPHash = session.IPHash,
                UserAgent = session.UserAgent,
                UpdatedAt = DateTime.Now,
                User = session.User,
                RefreshToken = CryptoService.GenerateRefreshToken()
            });
            return Ok(JsonSerializer.Serialize(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                accessToken = _cryptoService.CreateAccessJwt(HttpContext.Connection.RemoteIpAddress,
                    newSession.Entity.User.UserPID)
            }));
        }

        private class NumberBan
        {
            public Timer ExpirationTimer;
            public DateTime ExpiresAt;
            public string Number;
        }
    }
}