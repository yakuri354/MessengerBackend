using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using MessengerBackend.Models;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using UAParser;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserService _userService;
        private readonly VerificationService _verificationService;
        private readonly AuthService _authService;

        private readonly JwtBuilder _jwtBuilder;

        private List<NumberBan> _numberBans;

        public AuthController(
            UserService userService,
            VerificationService verificationService,
            AuthService authService)
        {
            _userService = userService;
            _verificationService = verificationService;
            _authService = authService;

            _jwtBuilder = new JwtBuilder()
                .WithAlgorithm(new RS256Algorithm(_authService.PublicKey, _authService.PrivateKey));
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

            if (input == null) return Forbid();

            if (input.code.Length != _verificationService.TwilioService.CodeLength) return Forbid();


            var result = await _verificationService.CheckVerificationAsync(input.number, input.code);
            if (!result.IsValid) return Forbid(result.Error);


            return Ok(JsonConvert.SerializeObject(new
            {
                registrationToken =
                    _jwtBuilder
                        .ExpirationTime(DateTime.Now.AddMinutes(30))
                        .AddClaim("type", "reg")
                        .AddClaim("num", input.number)
                        .Encode()
            }));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register()
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
            var token = _jwtBuilder
                .MustVerifySignature()
                .Decode<IDictionary<string, string>>(input.registerToken);
            if (token["type"] != "reg" || !token.ContainsKey("number"))
                return BadRequest(@"Token must have 'type' == 'reg'");

            var session =
                _authService.AddSession(new Session
                {
                    CreatedAt = DateTime.UtcNow,
                    ExpiresIn = AuthService.RefreshTokenLifetimeDays,
                    Fingerprint = input.fingerprint,
                    IP = HttpContext.Connection.RemoteIpAddress,
                    UserAgent = Request.Headers[HttpRequestHeader.UserAgent.ToString()],
                    User = _userService.Add(token["number"], input.firstName, input.lastName),
                    UpdatedAt = DateTime.UtcNow,
                    RefreshToken = AuthService.GenerateToken(AuthService.TokenLength)
                });

            return Ok(JsonConvert.SerializeObject(new
            {
                uid = newUser.PublicUID
            }));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var input = JsonConvert.DeserializeAnonymousType(Request.Body.GetString(), new
            {
                fingerprint = ""
            });
            var token = Request.Headers[HeaderNames.Authorization];
            string parsedToken;
            try
            {
                parsedToken = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(AuthOptions.JWTSECRET)
                    .MustVerifySignature()
                    .Decode(token);
            }
            catch (TokenExpiredException)
            {
                return Forbid(JsonConvert.SerializeObject(new
                {
                    error = "token expired"
                }));
            }
            catch (SignatureVerificationException)
            {
                return Forbid(JsonConvert.SerializeObject(new
                {
                    error = "token signature invalid"
                }));
            }

            // parsedToken.
            var session = await
                _authService.GetAndRefresh(token, input.fingerprint,
                    Request.Headers[HeaderNames.UserAgent]);
            if (session == null) return BadRequest();
            var newSession = new Session
            {
                CreatedAt = DateTime.Now,
                ExpiresIn = (DateTime.Now.AddDays(AuthService.RefreshTokenLifetimeDays) - DateTime.Now).Seconds,
                Fingerprint = input.fingerprint,
                IP = session.IP,
                UserAgent = session.UserAgent,
                UpdatedAt = DateTime.Now,
                User = session.User,
                // RefreshToken = new JwtSecurityToken()
                // issuer: AuthOptions.ISSUER,
                // audience: AuthOptions.AUDIENCE,
                // notBefore: DateTime.Now,
                // claims: new []
                // {
                //     new Claim("type", "refresh"),
                //     new Claim("user", ), 
                // }
                // )
            };
            return Ok();
        }

        private class NumberBan
        {
            public string Number;
            public Timer ExpirationTimer;
            public DateTime ExpiresAt;
        }
    }
}